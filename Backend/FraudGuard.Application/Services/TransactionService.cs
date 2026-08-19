using FraudGuard.Domain.Common.Constants;
using FraudGuard.Domain.Common.Enums;
using FraudGuard.Domain.DomainObjects;
using FraudGuard.Domain.DomainObjects.FraudEvaluation;
using FraudGuard.Domain.DomainObjects.TransactionProcessing;
using FraudGuard.Domain.Entities;
using FraudGuard.Domain.Interfaces.Abstractions;
using FraudGuard.Application.Interfaces;
using FraudGuard.Domain.Interfaces.DomainServices;
using FraudGuard.Domain.Interfaces.Repositories;
using System;
using System.Threading.Tasks;

namespace FraudGuard.Application.Services
{
    /// <summary>
    /// İşlem akışının orkestratörü: doğrulama, bakiye hareketi, fraud değerlendirmesi,
    /// kalıcılaştırma ve önbellek tazeleme adımlarını sırayla yürütür.
    /// <para>
    /// Akış iki ana dala ayrılır — transfer (IBAN) ve kart. Kart dalı ayrıca işlem tipine
    /// göre dört işleyiciye dağıtılır. Fraud değerlendirmesi dört tipte de aynı olduğu için
    /// tek bir yerde toplanmıştır.
    /// </para>
    /// </summary>
    public class TransactionService : ITransactionService
    {
        /// <summary>EBlockReason tablosundaki "Fraud" (Dolandırıcılık Şüphesi) kaydının kimliği.</summary>
        private const int FraudBlockReasonId = 2;


        private static readonly TimeSpan CardInfoCacheTtl = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan CvvFailWindow = TimeSpan.FromMinutes(30);
        private const int CvvFailThreshold = 3;

        private readonly ICreditCardRepository _creditCardRepository;
        private readonly IDebitCardRepository _debitCardRepository;
        private readonly ITransactionRepository _transactionRepository;
        private readonly IFraudEvaluationService _fraudEvaluationService;
        private readonly IBankAccountBeneficiaryRepository _bankAccountBeneficiaryRepository;
        private readonly ICurrencyService _currencyService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICacheProvider _cacheProvider;

        public TransactionService(
            ICreditCardRepository creditCardRepository,
            IDebitCardRepository debitCardRepository,
            ITransactionRepository transactionRepository,
            IFraudEvaluationService fraudEvaluationService,
            IBankAccountBeneficiaryRepository bankAccountBeneficiaryRepository,
            ICurrencyService currencyService,
            IUnitOfWork unitOfWork,
            ICacheProvider cacheProvider)
        {
            _creditCardRepository = creditCardRepository;
            _debitCardRepository = debitCardRepository;
            _transactionRepository = transactionRepository;
            _fraudEvaluationService = fraudEvaluationService;
            _bankAccountBeneficiaryRepository = bankAccountBeneficiaryRepository;
            _currencyService = currencyService;
            _unitOfWork = unitOfWork;
            _cacheProvider = cacheProvider;
        }

        public async Task<TransactionCheckResult> ProcessTransactionAsync(ProcessTransactionInput input)
        {
            bool isTransfer = input.PaymentType is PaymentTypeEnum.EFT or PaymentTypeEnum.BankTransfer;

            return isTransfer
                ? await ProcessTransferAsync(input)
                : await ProcessCardTransactionAsync(input);
        }

        // ==================================================================
        // Transfer akışı
        // ==================================================================

        private async Task<TransactionCheckResult> ProcessTransferAsync(ProcessTransactionInput input)
        {
            var result = new TransactionCheckResult();

            var senderDebit = await _debitCardRepository.GetByIBANAsync(input.SenderIBAN);
            if (senderDebit == null)
                return Decline(result, "Gönderici hesap bulunamadı.");

            if (senderDebit.IsBlocked)
                return Decline(result, "Gönderen kart/hesap blokeli.");

            // Not: "bekleyen şüpheli işlem varsa tümünü reddet" kontrolü kaldırıldı.
            // Kademeli karar modelinde İZLE "işleme izin ver, analiste bayrak düş" demektir;
            // eski kontrol İZLE'yi fiilen kalıcı bloke haline getiriyordu. Gerçek bloke
            // RET_BLOKE kararında uygulanır ve yukarıdaki IsBlocked kontrolü yakalar.

            decimal processedAmount = await _currencyService.ConvertToTryAsync(input.Amount, input.Currency);

            if (senderDebit.Balance < processedAmount)
                return Decline(result, "Yetersiz Bakiye.");

            var receiverDebit = await _debitCardRepository.GetByIBANAsync(input.ReceiverIBAN);

            if (receiverDebit == null && input.PaymentType == PaymentTypeEnum.BankTransfer)
                return Decline(result, "Alıcı hesap bulunamadı.");

            if (receiverDebit != null && !ReceiverNameMatches(receiverDebit, input.ReceiverName))
                return Decline(result, "Alıcı adı ve IBAN uyuşmuyor.");

            var evaluation = await _fraudEvaluationService.EvaluateAsync(
                input, senderDebit.CardId, isCreditCard: false);

            ApplyFraudDecision(result, evaluation);

            await ApplyTransferFundsAsync(input, result, evaluation, senderDebit, receiverDebit, processedAmount);

            var transferTx = BuildTransferTransaction(input, result, evaluation);
            await _transactionRepository.AddTransferTransactionAsync(transferTx);
            await _unitOfWork.SaveChangesAsync();

            await InvalidateTransferCachesAsync(input, senderDebit, receiverDebit);

            await RaiseFraudLogAsync(
                transferTx.TransactionId, evaluation.PrimaryRule?.RuleCode, input.PaymentType,
                evaluation.Decision, result.Status);

            result.TransactionId = transferTx.TransactionId;
            result.RRN = transferTx.RRN;
            return result;
        }

        private static bool ReceiverNameMatches(EDebitCard receiver, string? claimedName)
        {
            string actual = $"{receiver.Customer.FirstName} {receiver.Customer.LastName}".Trim();
            return string.Equals(actual, (claimedName ?? string.Empty).Trim(),
                StringComparison.CurrentCultureIgnoreCase);
        }

        /// <summary>
        /// Karara göre para hareketini uygular.
        /// RET'te tutar hiç çıkmaz; İZLE/EK_DOĞRULAMA'da gönderenden düşer ama alıcıya geçmez
        /// (inceleme bekler); NORMAL'de transfer tamamlanır ve alıcı kayıtlı hale gelir.
        /// </summary>
        private async Task ApplyTransferFundsAsync(
            ProcessTransactionInput input,
            TransactionCheckResult result,
            FraudDecisionResult evaluation,
            EDebitCard senderDebit,
            EDebitCard? receiverDebit,
            decimal processedAmount)
        {
            if (evaluation.ShouldBlock)
            {
                await BlockCardForFraudAsync(null, senderDebit);
                return;
            }

            if (evaluation.Decision != RiskDecisionEnum.Normal)
            {
                senderDebit.Balance -= processedAmount;
                await _debitCardRepository.UpdateAsync(senderDebit);
                return;
            }

            senderDebit.Balance -= processedAmount;

            if (receiverDebit != null)
            {
                receiverDebit.Balance += processedAmount;
                await _debitCardRepository.UpdateAsync(receiverDebit);
            }

            await _debitCardRepository.UpdateAsync(senderDebit);
            result.Status = TransactionStatuses.Approved;

            await RegisterBeneficiaryIfNewAsync(input, senderDebit);
        }

        private async Task RegisterBeneficiaryIfNewAsync(ProcessTransactionInput input, EDebitCard senderDebit)
        {
            bool known = await _bankAccountBeneficiaryRepository.AnyAsync(
                senderDebit.CustomerId, input.ReceiverIBAN);

            if (known)
                return;

            await _bankAccountBeneficiaryRepository.AddAsync(new EBankAccountBeneficiary
            {
                CustomerId = senderDebit.CustomerId,
                ReceiverIBAN = input.ReceiverIBAN,
                ReceiverName = input.ReceiverName ?? "Alıcı",
                AddedDate = DateTime.Now
            });
        }

        private ETransferTransaction BuildTransferTransaction(
            ProcessTransactionInput input, TransactionCheckResult result, FraudDecisionResult evaluation) =>
            new()
            {
                RRN = GenerateRrn(),
                SenderIBAN = input.SenderIBAN,
                ReceiverIBAN = input.ReceiverIBAN,
                ReceiverName = input.ReceiverName,
                Description = input.Description,
                ChannelTypeId = input.ChannelTypeId,
                Amount = input.Amount,
                Currency = input.Currency,
                TransactionDate = DateTime.Now,
                Location = input.Location ?? "İnternet Bankacılığı",
                Country = input.Country ?? "Türkiye",
                Status = result.Status,
                DeclineReason = result.DeclineReason,
                FraudReason = evaluation.BuildReasonSummary(),
                RiskScore = evaluation.FinalRiskScore,
                RiskDecision = evaluation.Decision
            };

        private async Task InvalidateTransferCachesAsync(
            ProcessTransactionInput input, EDebitCard senderDebit, EDebitCard? receiverDebit)
        {
            await _cacheProvider.RemoveAsync($"card_info_{senderDebit.CardNumber}");
            await _cacheProvider.RemoveAsync($"recent_txs_{senderDebit.CardNumber}");
            await _cacheProvider.RemoveAsync($"recent_txs_{input.SenderIBAN}");

            if (receiverDebit == null)
                return;

            await _cacheProvider.RemoveAsync($"card_info_{receiverDebit.CardNumber}");
            await _cacheProvider.RemoveAsync($"recent_txs_{receiverDebit.CardNumber}");
        }

        // ==================================================================
        // Kart akışı
        // ==================================================================

        /// <summary>
        /// Kart işlemi boyunca adımlar arasında taşınan durum.
        /// Uzun bir metot yerine işleyicilere bölündüğü için ortak durum burada toplanır.
        /// </summary>
        private sealed class CardContext
        {
            public required ProcessTransactionInput Input { get; init; }
            public required TransactionCheckResult Result { get; init; }
            public required string CacheKey { get; init; }

            public ECreditCard? CreditCard { get; init; }
            public EDebitCard? DebitCard { get; init; }
            public required bool IsCredit { get; init; }
            public required int CardId { get; init; }

            public decimal ProcessedAmount { get; set; }
            public bool IsCvvSuspicious { get; set; }

            /// <summary>
            /// İşleme atanan referans numarası. Bir kez üretilir: kayda yazılan ile
            /// yanıtta dönen aynı olmalıdır.
            /// </summary>
            public string AssignedRrn { get; set; } = string.Empty;

            // Fraud değerlendirmesinin işlem kaydına yazılacak çıktıları.
            public string? TriggeredRuleCode { get; set; }
            public string? FraudReason { get; set; }
            public int RiskScore { get; set; }
            public RiskDecisionEnum Decision { get; set; } = RiskDecisionEnum.Normal;
        }

        private async Task<TransactionCheckResult> ProcessCardTransactionAsync(ProcessTransactionInput input)
        {
            var result = new TransactionCheckResult();

            var context = await ResolveCardContextAsync(input, result);
            if (context == null)
                return result;

            await ApplyCvvVerificationAsync(context);

            context.ProcessedAmount = await _currencyService.ConvertToTryAsync(input.Amount, input.Currency);

            if (context.IsCvvSuspicious)
                SeedBruteForceOutcome(context);

            bool proceed = input.TransactionType switch
            {
                TransactionTypeEnum.Refund => await HandleRefundAsync(context),
                TransactionTypeEnum.Sale => await HandleSaleAsync(context),
                TransactionTypeEnum.Deposit => await HandleDepositAsync(context),
                TransactionTypeEnum.CardPayment => await HandleCardPaymentAsync(context),
                _ => true
            };

            if (!proceed)
                return result;

            context.AssignedRrn = input.TransactionType == TransactionTypeEnum.Refund
                ? input.RRN!
                : GenerateRrn();

            int transactionId = await PersistCardTransactionAsync(context);

            await _cacheProvider.RemoveAsync(context.CacheKey);
            await _cacheProvider.RemoveAsync($"recent_txs_{input.CardNumber}");

            await RaiseFraudLogAsync(
                transactionId, context.TriggeredRuleCode, input.PaymentType,
                context.Decision, result.Status);

            result.TransactionId = transactionId;
            result.RRN = context.AssignedRrn;
            return result;
        }

        /// <summary>
        /// Kartı bulur ve temel uygunluk kontrollerini yapar.
        /// Kart geçersiz veya blokeliyse null döner; çağıran sonucu olduğu gibi geri verir.
        /// </summary>
        private async Task<CardContext?> ResolveCardContextAsync(
            ProcessTransactionInput input, TransactionCheckResult result)
        {
            string cacheKey = $"card_info_{input.CardNumber}";
            var cachedCard = await _cacheProvider.GetAsync<CardCacheInfo>(cacheKey);

            var creditCard = await _creditCardRepository.GetByCardNumberAsync(input.CardNumber);
            var debitCard = creditCard == null
                ? await _debitCardRepository.GetByCardNumberAsync(input.CardNumber)
                : null;

            if (cachedCard == null)
            {
                if (creditCard != null)
                {
                    cachedCard = new CardCacheInfo
                    {
                        AvailableFunds = creditCard.AvailableLimit,
                        IsBlocked = creditCard.IsBlocked,
                        CVV = creditCard.CVV
                    };
                }
                else if (debitCard != null)
                {
                    cachedCard = new CardCacheInfo
                    {
                        AvailableFunds = debitCard.Balance,
                        IsBlocked = debitCard.IsBlocked,
                        CVV = debitCard.CVV
                    };
                }

                if (cachedCard != null)
                    await _cacheProvider.SetAsync(cacheKey, cachedCard, CardInfoCacheTtl);
            }

            if (cachedCard == null)
            {
                Decline(result, "Geçersiz Kart");
                return null;
            }

            if (cachedCard.IsBlocked)
            {
                Decline(result, "Kart Blokeli");
                return null;
            }

            bool isCredit = creditCard != null;

            return new CardContext
            {
                Input = input,
                Result = result,
                CacheKey = cacheKey,
                CreditCard = creditCard,
                DebitCard = debitCard,
                IsCredit = isCredit,
                CardId = isCredit ? creditCard!.CardId : debitCard!.CardId
            };
        }

        /// <summary>
        /// CVV doğrulaması ve ardışık hatalı deneme sayacı.
        /// Eşiğe ulaşıldığında işlem kural motorundan bağımsız olarak şüpheli işaretlenir.
        /// </summary>
        private async Task ApplyCvvVerificationAsync(CardContext ctx)
        {
            string expectedCvv = ctx.IsCredit ? ctx.CreditCard!.CVV : ctx.DebitCard!.CVV;
            string cvvFailKey = $"cvv_fail_cnt_{ctx.Input.CardNumber}";

            if (expectedCvv == ctx.Input.CVV)
            {
                ctx.Result.Status = TransactionStatuses.Approved;
                await _cacheProvider.RemoveAsync(cvvFailKey);
                return;
            }

            int failCount = (await _cacheProvider.GetAsync<int>(cvvFailKey)) + 1;
            await _cacheProvider.SetAsync(cvvFailKey, failCount, CvvFailWindow);

            if (failCount < CvvFailThreshold)
            {
                Decline(ctx.Result, "Hatalı CVV");
                return;
            }

            ctx.IsCvvSuspicious = true;
            ctx.Result.Status = TransactionStatuses.Suspicious;
            ctx.Result.DeclineReason = "Fraud Şüphesi: BRUTE_FORCE";
            await _cacheProvider.RemoveAsync(cvvFailKey);
        }

        /// <summary>
        /// CVV brute-force kontrolü kural motorundan önce ve ondan bağımsız çalışır;
        /// motorun ürettiği bir skoru yoktur. Mevcut davranışı (analiste düşen şüpheli işlem)
        /// korumak için İZLE eşiğine sabitlenir.
        /// </summary>
        private static void SeedBruteForceOutcome(CardContext ctx)
        {
            ctx.TriggeredRuleCode = "BRUTE_FORCE";
            ctx.FraudReason = "3 kez üst üste hatalı CVV denemesi yapılmıştır.";
            ctx.RiskScore = RiskScoringConstants.IzleThreshold;
            ctx.Decision = RiskDecisionEnum.Izle;
        }

        // ------------------------------------------------------------------
        // İşlem tipi işleyicileri — false dönerse akış kesilir, sonuç hazırdır
        // ------------------------------------------------------------------

        private async Task<bool> HandleRefundAsync(CardContext ctx)
        {
            var input = ctx.Input;

            if (string.IsNullOrEmpty(input.RRN))
                return Stop(ctx, "İade işlemi için RRN değeri belirtilmelidir.");

            var originalTx = await _transactionRepository.GetOriginalSaleByRrnAsync(
                input.RRN, ctx.CardId, ctx.IsCredit);

            if (originalTx == null)
                return Stop(ctx, "Orijinal satış işlemi bulunamadı.");

            if (await _transactionRepository.HasBeenRefundedAsync(input.RRN, ctx.CardId, ctx.IsCredit))
                return Stop(ctx, "Bu işlem zaten iade edilmiştir.");

            if (originalTx.Amount != input.Amount || originalTx.Currency != input.Currency)
                return Stop(ctx, "İade tutarı veya para birimi orijinal işlem ile eşleşmiyor.");

            if (ctx.IsCredit)
            {
                ctx.CreditCard!.AvailableLimit = Math.Min(
                    ctx.CreditCard.AvailableLimit + ctx.ProcessedAmount, ctx.CreditCard.CardLimit);
                await _creditCardRepository.UpdateAsync(ctx.CreditCard);
            }
            else
            {
                ctx.DebitCard!.Balance += ctx.ProcessedAmount;
                await _debitCardRepository.UpdateAsync(ctx.DebitCard);
            }

            await _cacheProvider.RemoveAsync(ctx.CacheKey);
            await _cacheProvider.RemoveAsync($"recent_txs_{input.CardNumber}");

            if (!ctx.IsCvvSuspicious)
                await EvaluateFraudAsync(ctx);

            return true;
        }

        private async Task<bool> HandleSaleAsync(CardContext ctx)
        {
            bool declinedEarlier = ctx.Result.Status == TransactionStatuses.Declined;

            decimal availableFunds = ctx.IsCredit
                ? ctx.CreditCard!.AvailableLimit
                : ctx.DebitCard!.Balance;

            if (!declinedEarlier && !ctx.IsCvvSuspicious && availableFunds < ctx.ProcessedAmount)
            {
                Decline(ctx.Result, "Yetersiz Bakiye");
                declinedEarlier = true;
            }

            if (declinedEarlier || ctx.IsCvvSuspicious)
                return true;

            var evaluation = await EvaluateFraudAsync(ctx);

            // RET kararında tutar tahsil edilmez; kart zaten bloke edilmiştir.
            if (evaluation.ShouldBlock)
                return true;

            if (ctx.IsCredit)
                ctx.CreditCard!.AvailableLimit -= ctx.ProcessedAmount;
            else
                ctx.DebitCard!.Balance -= ctx.ProcessedAmount;

            return true;
        }

        private async Task<bool> HandleDepositAsync(CardContext ctx)
        {
            if (ctx.IsCredit)
            {
                return Stop(ctx,
                    "Kredi kartına doğrudan para yatırılamaz. Lütfen Kredi Kartı Borç Ödeme seçeneğini kullanın.");
            }

            ctx.DebitCard!.Balance += ctx.ProcessedAmount;
            await _debitCardRepository.UpdateAsync(ctx.DebitCard);

            ctx.Result.Status = TransactionStatuses.Approved;
            await _cacheProvider.RemoveAsync(ctx.CacheKey);
            await _cacheProvider.RemoveAsync($"recent_txs_{ctx.Input.CardNumber}");

            if (!ctx.IsCvvSuspicious)
                await EvaluateFraudAsync(ctx);

            return true;
        }

        private async Task<bool> HandleCardPaymentAsync(CardContext ctx)
        {
            if (!ctx.IsCredit)
                return Stop(ctx, "Banka kartı için borç ödeme işlemi yapılamaz.");

            decimal currentDebt = ctx.CreditCard!.CardLimit - ctx.CreditCard.AvailableLimit;

            if (ctx.ProcessedAmount > currentDebt)
            {
                return Stop(ctx,
                    $"Borcunuz {currentDebt:N2} {ctx.Input.Currency}'dir, fazla ödeme yapmayı denediniz.");
            }

            ctx.CreditCard.AvailableLimit += ctx.ProcessedAmount;
            await _creditCardRepository.UpdateAsync(ctx.CreditCard);

            ctx.Result.Status = TransactionStatuses.Approved;
            await _cacheProvider.RemoveAsync(ctx.CacheKey);
            await _cacheProvider.RemoveAsync($"recent_txs_{ctx.Input.CardNumber}");

            if (!ctx.IsCvvSuspicious)
                await EvaluateFraudAsync(ctx);

            return true;
        }

        /// <summary>
        /// Kural motorunu çalıştırır, kararı sonuca yansıtır ve RET'te kartı bloke eder.
        /// Dört işlem tipinde de aynı olduğu için tek yerde toplanmıştır.
        /// </summary>
        private async Task<FraudDecisionResult> EvaluateFraudAsync(CardContext ctx)
        {
            var evaluation = await _fraudEvaluationService.EvaluateAsync(
                ctx.Input, ctx.CardId, ctx.IsCredit);

            ctx.TriggeredRuleCode = evaluation.PrimaryRule?.RuleCode;
            ctx.FraudReason = evaluation.BuildReasonSummary();
            ctx.RiskScore = evaluation.FinalRiskScore;
            ctx.Decision = evaluation.Decision;

            ctx.Result.Status = TransactionStatuses.Approved;
            ApplyFraudDecision(ctx.Result, evaluation);

            if (evaluation.ShouldBlock)
                await BlockCardForFraudAsync(ctx.CreditCard, ctx.DebitCard);

            return evaluation;
        }

        private async Task<int> PersistCardTransactionAsync(CardContext ctx)
        {
            var input = ctx.Input;
            string assignedRrn = ctx.AssignedRrn;

            string? declineReason = ctx.Result.Status == TransactionStatuses.Suspicious
                ? $"Fraud: {ctx.TriggeredRuleCode}"
                : ctx.Result.DeclineReason;

            if (ctx.IsCredit)
            {
                var ccTx = new ECreditCardTransaction
                {
                    CreditCardId = ctx.CardId,
                    TransactionTypeId = (int)input.TransactionType,
                    ChannelTypeId = input.ChannelTypeId == 0 ? 2 : input.ChannelTypeId,
                    Amount = input.Amount,
                    Currency = input.Currency,
                    TransactionDate = DateTime.Now,
                    Location = input.Location,
                    Country = input.Country,
                    MerchantCategory = input.MerchantCategory,
                    MerchantId = input.MerchantId,
                    Status = ctx.Result.Status,
                    DeclineReason = declineReason,
                    FraudReason = ctx.FraudReason,
                    RiskScore = ctx.RiskScore,
                    RiskDecision = ctx.Decision,
                    RRN = assignedRrn
                };

                await _transactionRepository.AddCreditCardTransactionAsync(ccTx);
                await _creditCardRepository.UpdateAsync(ctx.CreditCard!);
                await _unitOfWork.SaveChangesAsync();
                return ccTx.TransactionId;
            }

            var dcTx = new EDebitCardTransaction
            {
                DebitCardId = ctx.CardId,
                TransactionTypeId = (int)input.TransactionType,
                ChannelTypeId = input.ChannelTypeId == 0 ? 2 : input.ChannelTypeId,
                Amount = input.Amount,
                Currency = input.Currency,
                TransactionDate = DateTime.Now,
                Location = input.Location,
                Country = input.Country,
                MerchantCategory = input.MerchantCategory,
                MerchantId = input.MerchantId,
                Status = ctx.Result.Status,
                DeclineReason = declineReason,
                FraudReason = ctx.FraudReason,
                RiskScore = ctx.RiskScore,
                RiskDecision = ctx.Decision,
                RRN = assignedRrn
            };

            await _transactionRepository.AddDebitCardTransactionAsync(dcTx);
            await _debitCardRepository.UpdateAsync(ctx.DebitCard!);
            await _unitOfWork.SaveChangesAsync();
            return dcTx.TransactionId;
        }

        // ==================================================================
        // Ortak yardımcılar
        // ==================================================================

        /// <summary>
        /// 12 haneli referans numarası üretir.
        /// <c>Random.Shared</c> kullanılır: her çağrıda <c>new Random()</c> oluşturmak,
        /// aynı milisaniyede gelen isteklerde aynı diziyi üretebilirdi.
        /// </summary>
        private static string GenerateRrn() =>
            DateTime.UtcNow.ToString("yyMMdd") + Random.Shared.Next(100000, 999999).ToString("D6");

        private static TransactionCheckResult Decline(TransactionCheckResult result, string reason)
        {
            result.Status = TransactionStatuses.Declined;
            result.DeclineReason = reason;
            return result;
        }

        /// <summary>Akışı erken sonlandırır: sonucu reddedilmiş işaretler ve devam etmemeyi bildirir.</summary>
        private static bool Stop(CardContext ctx, string reason)
        {
            Decline(ctx.Result, reason);
            return false;
        }

        /// <summary>
        /// Fraud motorunun kademeli kararını işlem sonucuna yansıtır.
        /// RET_BLOKE reddedilir; İZLE ve EK_DOGRULAMA şüpheli olarak kaydedilip analiste düşer.
        /// </summary>
        private static void ApplyFraudDecision(TransactionCheckResult result, FraudDecisionResult evaluation)
        {
            result.FraudDecision = evaluation;
            result.IsSuspicious = evaluation.IsSuspicious;
            result.TriggeredRuleName = evaluation.PrimaryRule?.RuleName ?? string.Empty;

            if (!evaluation.IsSuspicious)
                return;

            // NORMAL kademesi "işleme izin ver" demektir. Kural tetiklenmiş olsa bile
            // skor eşiğin altındaysa işlem şüpheli işaretlenmez; yalnızca gerekçe kaydedilir.
            if (evaluation.Decision == RiskDecisionEnum.Normal)
                return;

            if (evaluation.ShouldBlock)
            {
                Decline(result, $"Fraud RET — Risk skoru {evaluation.FinalRiskScore}");
                return;
            }

            result.Status = TransactionStatuses.Suspicious;
            result.DeclineReason = evaluation.Decision == RiskDecisionEnum.EkDogrulama
                ? $"Ek doğrulama gerekli (3D/OTP) — Risk skoru {evaluation.FinalRiskScore}"
                : $"Fraud Şüphesi — Risk skoru {evaluation.FinalRiskScore}";
        }

        /// <summary>RET_BLOKE kararında kartı fraud gerekçesiyle bloke eder.</summary>
        private async Task BlockCardForFraudAsync(ECreditCard? creditCard, EDebitCard? debitCard)
        {
            if (creditCard != null)
            {
                creditCard.Block(FraudBlockReasonId);
                await _creditCardRepository.UpdateAsync(creditCard);
            }
            else if (debitCard != null)
            {
                debitCard.Block(FraudBlockReasonId);
                await _debitCardRepository.UpdateAsync(debitCard);
            }
        }

        /// <summary>
        /// Alarm yalnızca karar NORMAL'in üzerindeyse açılır; onaylanan işlem analist
        /// kuyruğuna girmez. Otomatik bloke edilen işlemler çözülmüş olarak işaretlenir.
        /// </summary>
        private async Task RaiseFraudLogAsync(
            int transactionId,
            string? ruleCode,
            PaymentTypeEnum paymentType,
            RiskDecisionEnum decision,
            string status)
        {
            if (ruleCode == null || status == TransactionStatuses.Approved)
                return;

            bool isAutoBlocked = decision == RiskDecisionEnum.RetBloke || status == TransactionStatuses.Declined;

            await _fraudEvaluationService.CreateFraudLogAsync(
                transactionId,
                ruleCode,
                paymentType,
                isAutoBlocked: isAutoBlocked,
                resolvedBy: isAutoBlocked ? "Sistem (Otomatik Bloke)" : null,
                adminNote: isAutoBlocked ? "Sistem tarafından şüpheli bulunup direkt bloke edildi." : null);

            await _unitOfWork.SaveChangesAsync();
        }
    }
}
