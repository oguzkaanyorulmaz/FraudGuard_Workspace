using AutoMapper;
using FraudGuard.Application.DTOs;
using FraudGuard.Application.DTOs.RuleManagement;
using FraudGuard.Application.Interfaces;
using FraudGuard.Domain.Common.Enums;
using FraudGuard.Domain.DomainObjects.TransactionProcessing;
using FraudGuard.Domain.Entities;
using FraudGuard.Domain.Interfaces.Abstractions;
using FraudGuard.Domain.Interfaces.Repositories;
using FraudGuard.Domain.Services.RuleEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FraudGuard.Application.Services
{
    public class RuleManagementAppService : IRuleManagementAppService
    {
        private const string RootGroup = "Temel";

        private const string CallerSuppliedNote =
            "İstekte auth bloğu gönderilmedikçe null kalır; bu alanı kullanan kural o işlemde tetiklenmez.";

        private readonly IFraudRuleRepository _fraudRuleRepository;
        private readonly IFraudLogRepository _fraudLogRepository;
        private readonly IRuleExpressionCompiler _expressionCompiler;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        /// <summary>
        /// Çalışma anında doldurulmayan alanların listesi.
        /// <para>
        /// Kaynağı <see cref="TransactionInputEnricher"/>'dır: bir alanın dolup dolmadığı onun
        /// davranışıdır, bu servisin bilgisi değil. Burada kopyası tutulsaydı ikisi zamanla
        /// birbirinden kayar ve arayüz ölü bir alanı çalışıyormuş gibi gösterirdi.
        /// </para>
        /// </summary>
        private static IReadOnlyDictionary<string, string> UnpopulatedFields =>
            TransactionInputEnricher.UnpopulatedFields;

        public RuleManagementAppService(
            IFraudRuleRepository fraudRuleRepository,
            IFraudLogRepository fraudLogRepository,
            IRuleExpressionCompiler expressionCompiler,
            IUnitOfWork unitOfWork,
            IMapper mapper)
        {
            _fraudRuleRepository = fraudRuleRepository;
            _fraudLogRepository = fraudLogRepository;
            _expressionCompiler = expressionCompiler;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<ResponseDTO<List<GetActiveRulesResponse>>> GetActiveRulesAsync()
        {
            var rules = await _fraudRuleRepository.GetAllActiveRulesAsync();
            return ResponseDTO<List<GetActiveRulesResponse>>.Success(
                _mapper.Map<List<GetActiveRulesResponse>>(rules));
        }

        public async Task<ResponseDTO<List<GetActiveRulesResponse>>> GetAllRulesAsync()
        {
            var rules = await _fraudRuleRepository.GetAllAsync();
            return ResponseDTO<List<GetActiveRulesResponse>>.Success(
                _mapper.Map<List<GetActiveRulesResponse>>(rules));
        }

        public Task<ResponseDTO<ValidateExpressionResponse>> ValidateExpressionAsync(
            ValidateExpressionRequest request)
        {
            bool isValid = _expressionCompiler.TryValidate(request.Expression, out var error);

            var response = new ValidateExpressionResponse
            {
                IsValid = isValid,
                Error = error
            };

            return Task.FromResult(ResponseDTO<ValidateExpressionResponse>.Success(
                response,
                isValid ? "İfade geçerli." : "İfade derlenemedi."));
        }

        public async Task<ResponseDTO<CreateFraudRuleResponse>> CreateRuleAsync(CreateFraudRuleRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.RuleCode))
                return ResponseDTO<CreateFraudRuleResponse>.Fail("Kural kodu zorunludur.");

            if (string.IsNullOrWhiteSpace(request.RuleName))
                return ResponseDTO<CreateFraudRuleResponse>.Fail("Kural adı zorunludur.");

            if (string.IsNullOrWhiteSpace(request.Expression))
                return ResponseDTO<CreateFraudRuleResponse>.Fail("İfade zorunludur.");

            if (request.Score <= 0)
                return ResponseDTO<CreateFraudRuleResponse>.Fail("Puan 0'dan büyük olmalıdır.");

            if (!Enum.TryParse<RuleTargetEnum>(request.Target, ignoreCase: true, out var target))
                return ResponseDTO<CreateFraudRuleResponse>.Fail("Hedef 'Card' veya 'Merchant' olmalıdır.");

            if (!Enum.TryParse<RuleCategoryEnum>(request.Category, ignoreCase: true, out var category))
                return ResponseDTO<CreateFraudRuleResponse>.Fail(
                    "Kategori 'Velocity', 'Amount', 'Time', 'Identity' veya 'Location' olmalıdır.");

            // İfade derlenmeden kural kaydedilmez — sessizce ölü kural oluşmasını engeller.
            if (!_expressionCompiler.TryValidate(request.Expression, out var expressionError))
                return ResponseDTO<CreateFraudRuleResponse>.Fail($"İfade derlenemedi: {expressionError}");

            if (await _fraudRuleRepository.ExistsByCodeAsync(request.RuleCode))
                return ResponseDTO<CreateFraudRuleResponse>.Fail(
                    $"'{request.RuleCode}' kodlu bir kural zaten var.");

            var rule = new EFraudRule
            {
                RuleCode = request.RuleCode.Trim(),
                RuleName = request.RuleName.Trim(),
                Description = request.Description?.Trim(),
                Expression = request.Expression.Trim(),
                Score = request.Score,
                Target = target,
                Category = category,
                IsCritical = request.IsCritical,
                IsActive = request.IsActive
            };

            await _fraudRuleRepository.AddAsync(rule);
            await _unitOfWork.SaveChangesAsync();

            return ResponseDTO<CreateFraudRuleResponse>.Success(
                new CreateFraudRuleResponse
                {
                    RuleId = rule.RuleId,
                    RuleCode = rule.RuleCode,
                    Message = rule.IsActive
                        ? "Kural eklendi ve bir sonraki işlemden itibaren aktif."
                        : "Kural eklendi, pasif durumda."
                },
                "Kural oluşturuldu.");
        }

        public async Task<ResponseDTO<RuleMutationResponse>> DeleteRuleAsync(int ruleId)
        {
            var rule = await _fraudRuleRepository.GetByIdAsync(ruleId);
            if (rule is null)
                return ResponseDTO<RuleMutationResponse>.Fail($"{ruleId} numaralı kural bulunamadı.");

            // Fraud logları RuleId üzerinden kurala bağlı ve FK'sı Restrict.
            // Silmeye kalkışmak veritabanı hatası verir; öncesinde anlaşılır bir yanıt döndürüyoruz.
            if (await _fraudLogRepository.AnyByRuleIdAsync(ruleId))
            {
                return ResponseDTO<RuleMutationResponse>.Fail(
                    $"'{rule.RuleCode}' kuralı geçmiş fraud alarmlarına bağlı olduğu için silinemez. " +
                    "Kuralı devre dışı bırakmak için pasife alın.");
            }

            _fraudRuleRepository.Delete(rule);
            await _unitOfWork.SaveChangesAsync();

            return ResponseDTO<RuleMutationResponse>.Success(
                new RuleMutationResponse
                {
                    RuleId = rule.RuleId,
                    RuleCode = rule.RuleCode,
                    IsActive = false
                },
                $"'{rule.RuleCode}' kuralı silindi.");
        }

        public async Task<ResponseDTO<RuleMutationResponse>> SetRuleStatusAsync(
            int ruleId, SetRuleStatusRequest request)
        {
            var rule = await _fraudRuleRepository.GetByIdAsync(ruleId);
            if (rule is null)
                return ResponseDTO<RuleMutationResponse>.Fail($"{ruleId} numaralı kural bulunamadı.");

            rule.IsActive = request.IsActive;
            await _unitOfWork.SaveChangesAsync();

            return ResponseDTO<RuleMutationResponse>.Success(
                new RuleMutationResponse
                {
                    RuleId = rule.RuleId,
                    RuleCode = rule.RuleCode,
                    IsActive = rule.IsActive
                },
                rule.IsActive
                    ? $"'{rule.RuleCode}' kuralı aktifleştirildi, bir sonraki işlemden itibaren devrede."
                    : $"'{rule.RuleCode}' kuralı pasife alındı, artık değerlendirilmeyecek.");
        }

        /// <summary>
        /// İfadelerde kullanılabilecek alanların tam listesi.
        /// <para>
        /// İç içe nesneler (örn. <c>Auth</c>) tek satır olarak değil, alanları
        /// <c>Auth.PinExist</c> biçiminde açılarak listelenir; nesnenin kendisi bir ifadede
        /// doğrudan kullanılamayacağı için satır olarak dönmez.
        /// </para>
        /// </summary>
        public ResponseDTO<List<RuleFieldDto>> GetAvailableFields()
        {
            var fields = new List<RuleFieldDto>();

            foreach (var property in typeof(ProcessTransactionInput).GetProperties().Where(p => p.CanRead))
            {
                if (IsNestedGroup(property.PropertyType))
                {
                    AppendNestedFields(fields, property);
                    continue;
                }

                bool unpopulated = UnpopulatedFields.TryGetValue(property.Name, out var note);

                fields.Add(new RuleFieldDto
                {
                    Name = property.Name,
                    Type = FormatTypeName(property.PropertyType),
                    Group = RootGroup,
                    IsPopulated = !unpopulated,
                    Note = note
                });
            }

            var ordered = fields
                .OrderBy(f => f.Group == RootGroup ? 0 : 1)
                .ThenByDescending(f => f.IsPopulated)
                .ThenBy(f => f.Name, StringComparer.Ordinal)
                .ToList();

            return ResponseDTO<List<RuleFieldDto>>.Success(ordered);
        }

        private static void AppendNestedFields(List<RuleFieldDto> fields, System.Reflection.PropertyInfo group)
        {
            foreach (var nested in group.PropertyType.GetProperties().Where(p => p.CanRead))
            {
                fields.Add(new RuleFieldDto
                {
                    Name = $"{group.Name}.{nested.Name}",
                    Type = FormatTypeName(nested.PropertyType),
                    Group = group.Name,
                    // Bu alanlar enricher tarafından değil, isteği gönderen tarafından doldurulur.
                    // Yapısal olarak ölü değiller; bu yüzden IsPopulated = true, açıklama notta.
                    IsPopulated = true,
                    Note = CallerSuppliedNote
                });
            }
        }

        /// <summary>
        /// Alanları ayrı ayrı listelenmesi gereken iç içe nesne mi.
        /// string ve değer tipleri kendi başına bir alandır, açılmaz.
        /// </summary>
        private static bool IsNestedGroup(Type type) =>
            type.IsClass && type != typeof(string);

        private static string FormatTypeName(Type type)
        {
            var underlying = Nullable.GetUnderlyingType(type);
            string name = (underlying ?? type).Name;
            return underlying is null ? name : $"{name}?";
        }
    }
}
