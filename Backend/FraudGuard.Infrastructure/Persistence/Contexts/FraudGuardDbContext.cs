using FraudGuard.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Reflection;
using System.Collections.Generic;
using FraudGuard.Domain.Common.Enums;

namespace FraudGuard.Infrastructure.Persistence.Contexts
{
    public class FraudGuardDbContext : DbContext
    {
        public FraudGuardDbContext(DbContextOptions<FraudGuardDbContext> options) : base(options) { }

        public DbSet<ECustomer> Customers { get; set; }
        public DbSet<ECreditCard> CreditCards { get; set; }
        public DbSet<ETransaction> Transactions { get; set; }
        public DbSet<EDebitCard> DebitCards { get; set; }
        public DbSet<EChannelType> ChannelTypes { get; set; }
        public DbSet<EBankAccountBeneficiary> BankAccountBeneficiaries { get; set; }
        public DbSet<ETransactionType> TransactionTypes { get; set; } 
        public DbSet<EPaymentType> PaymentTypes { get; set; } 
        public DbSet<EUser> Users { get; set; }

        public DbSet<EFraudRule> FraudRules { get; set; }
        public DbSet<EFraudLog> FraudLogs { get; set; }
        public DbSet<EBlockReason> BlockReasons { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
            base.OnModelCreating(modelBuilder);
            
            modelBuilder.Entity<EFraudLog>()
                .ToTable(tb => tb.HasTrigger("trg_AfterFraudLogUpdate"));

            // 1. Transaction Types Seeding
            modelBuilder.Entity<ETransactionType>().HasData(
                new ETransactionType { TransactionTypeId = 1, TypeCode = "Sale", Description = "Satış İşlemi" },
                new ETransactionType { TransactionTypeId = 2, TypeCode = "Refund", Description = "İade İşlemi" },
                new ETransactionType { TransactionTypeId = 3, TypeCode = "Void", Description = "İptal İşlemi" },
                new ETransactionType { TransactionTypeId = 4, TypeCode = "Transfer", Description = "Para Gönderimi" }
            );

            // 2. Fraud Rules Seeding
            modelBuilder.Entity<EFraudRule>().HasData(
                new EFraudRule { RuleId = 1, RuleCode = "VELOCITY", RuleName = "Hız / Sıklık Kuralı", Description = "Belirli bir zaman dilimi içinde peş peşe yapılan işlemler", IsActive = true },
                new EFraudRule { RuleId = 2, RuleCode = "IMPOSSIBLE_TRAVEL", RuleName = "İmkansız Seyahat", Description = "Fiziksel olarak mümkün olmayan süre ve mesafelerdeki işlemler", IsActive = true },
                new EFraudRule { RuleId = 3, RuleCode = "ANOMALOUS_TIME", RuleName = "Zaman ve Tutar Sapması", Description = "Gece geç saatlerde yapılan olağandışı yüksek tutarlı işlemler", IsActive = true },
                new EFraudRule { RuleId = 4, RuleCode = "CARD_TESTING", RuleName = "Kart Deneme / Yoklama", Description = "Kartın aktifliğini doğrulamak amacıyla yapılan küçük tutarlı denemeler", IsActive = true },
                new EFraudRule { RuleId = 5, RuleCode = "BRUTE_FORCE", RuleName = "Ardışık Red", Description = "Kısa süre içinde üst üste alınan işlem reddi durumları", IsActive = true },
                new EFraudRule { RuleId = 6, RuleCode = "CROSS_BORDER", RuleName = "Sınır Ötesi İlk İşlem", Description = "Kart geçmişinde hiç bulunmayan bir ülkeden yapılan harcamalar", IsActive = true },
                new EFraudRule { RuleId = 7, RuleCode = "HIGH_RISK_MCC", RuleName = "Yüksek Riskli Üye İşyeri", Description = "Kuyumcu, şans oyunları, kripto borsaları gibi riskli kategorilerden yapılan işlemler", IsActive = true },
                new EFraudRule { RuleId = 8, RuleCode = "MAX_OUT", RuleName = "Limit Boşaltma Denemesi", Description = "Kart limitinin tamamına yakınını (%95+) tek seferde harcama denemesi", IsActive = true },
                new EFraudRule { RuleId = 9, RuleCode = "CURRENCY_MISMATCH", RuleName = "Para Birimi Sapması", Description = "Müşterinin kart geçmişinde bulunmayan bir para birimiyle işlem denemesi", IsActive = true },
                new EFraudRule { RuleId = 10, RuleCode = "CONSECUTIVE_REFUNDS", RuleName = "Ardışık İade Kuralı", Description = "Kısa süre içerisinde üst üste yapılan şüpheli iade (Refund) işlemleri denemesi", IsActive = true },
                new EFraudRule { RuleId = 11, RuleCode = "SMURFING", RuleName = "Smurfing (Dilimleme)", Description = "Son 1 saatteki transferlerin bildirim limitini aşması", IsActive = true },
                new EFraudRule { RuleId = 12, RuleCode = "WALLET_CASHOUT", RuleName = "Wallet Cash-Out", Description = "Cüzdan fonlanmasından hemen sonra EFT ile çıkış yapılması", IsActive = true },
                new EFraudRule { RuleId = 13, RuleCode = "MULTI_SOURCE_FUNDING", RuleName = "Çoklu Kaynakla Fonlama", Description = "Aynı cüzdana kısa sürede farklı kartlarla bakiye yüklenmesi", IsActive = true },
                new EFraudRule { RuleId = 14, RuleCode = "CROSS_BORDER_TRANSFER", RuleName = "Sınır Ötesi Transfer", Description = "İlk defa yurt dışı IBAN'a yüksek tutarlı EFT yollanması", IsActive = true },
                new EFraudRule { RuleId = 15, RuleCode = "ACCOUNT_DRAIN", RuleName = "Hesap Boşaltma Denemesi", Description = "Tek işlemde hesap bakiyesinin %98 ve üzerini çekme denemesi", IsActive = true },
                new EFraudRule { RuleId = 16, RuleCode = "NEW_BENEFICIARY_TRANSFER", RuleName = "Yeni Alıcı Transfer Anormalliği", Description = "Yeni eklenen alıcıya ilk 5 dakikada yüksek transfer yapılması", IsActive = true },
                new EFraudRule { RuleId = 17, RuleCode = "SUSPICIOUS_DESCRIPTION", RuleName = "Şüpheli İşlem Açıklaması", Description = "Açıklamada bahis, kripto vb. yasaklı kelimelerin bulunması", IsActive = true },
                new EFraudRule { RuleId = 18, RuleCode = "HIGH_RISK_RECEIVER", RuleName = "Şüpheli Alıcı/Katır Hesap", Description = "Gönderilen IBAN'ın sistemde kara listede olması", IsActive = true },
                new EFraudRule { RuleId = 19, RuleCode = "MULTI_SENDER_TO_SINGLE_RECEIVER", RuleName = "Tek Alıcıya Çoklu Gönderim", Description = "Aynı alıcıya kısa sürede farklı kişilerden para transferi", IsActive = true },
                new EFraudRule { RuleId = 20, RuleCode = "RECEIVER_BALANCE_ANOMALY", RuleName = "Katır Hesap Bakiye Sapması", Description = "Pasif hesaba ani bakiye gelip 1 saatte nakit çekilmeye çalışılması", IsActive = true }
            );

            // 3. Block Reasons Seeding
            modelBuilder.Entity<EBlockReason>().HasData(
                new EBlockReason { ReasonId = 1, ReasonCode = "Stolen", Description = "Çalıntı" },
                new EBlockReason { ReasonId = 2, ReasonCode = "Fraud", Description = "Dolandırıcılık Şüphesi" },
                new EBlockReason { ReasonId = 3, ReasonCode = "Lost", Description = "Kayıp" }
            );

            // 4. Payment Types Seeding
            modelBuilder.Entity<EPaymentType>().HasData(
                new EPaymentType { PaymentTypeId = 1, TypeCode = "CreditCard", Description = "Kredi Kartı" },
                new EPaymentType { PaymentTypeId = 2, TypeCode = "DebitCard", Description = "Banka Kartı" },
                new EPaymentType { PaymentTypeId = 3, TypeCode = "BankTransfer", Description = "Havale" },
                new EPaymentType { PaymentTypeId = 4, TypeCode = "EFT", Description = "EFT" },
                new EPaymentType { PaymentTypeId = 5, TypeCode = "DigitalWallet", Description = "Dijital Cüzdan" }
            );

            // 5. Customers Seeding (1-20)
            var customers = new List<ECustomer>();
            for (int i = 1; i <= 20; i++)
            {
                customers.Add(new ECustomer
                {
                    CustomerId = i,
                    FirstName = $"Musteri{i}",
                    LastName = "Soyad",
                    IdentityNumber = (10000000000 + i).ToString(),
                    PhoneNumber = $"+9055555555{i:D2}",
                    Email = $"customer{i}@mail.com",
                    CreatedAt = new DateTime(2026, 7, 16)
                });
            }
            modelBuilder.Entity<ECustomer>().HasData(customers);

            // 6. Credit Cards Seeding (1-20)
            var creditCards = new List<ECreditCard>();
            for (int i = 1; i <= 20; i++)
            {
                creditCards.Add(new ECreditCard
                {
                    CardId = i,
                    CustomerId = i,
                    CardNumber = $"552000000000{i:D4}",
                    ExpiryDate = "12/28",
                    CVV = $"{100 + i}",
                    CardLimit = 150000,
                    AvailableLimit = 120000,
                    IsBlocked = false,
                    BlockReasonId = null
                });
            }
            modelBuilder.Entity<ECreditCard>().HasData(creditCards);

            // 7. Channel Types Seeding
            modelBuilder.Entity<EChannelType>().HasData(
                new EChannelType { ChannelTypeId = 1, ChannelCode = "POS", Description = "Fiziksel POS" },
                new EChannelType { ChannelTypeId = 2, ChannelCode = "VirtualPOS", Description = "Sanal POS" },
                new EChannelType { ChannelTypeId = 3, ChannelCode = "ATM", Description = "ATM Cihazı" },
                new EChannelType { ChannelTypeId = 4, ChannelCode = "Mobile", Description = "Mobil Şube" },
                new EChannelType { ChannelTypeId = 5, ChannelCode = "Web", Description = "İnternet Şubesi" }
            );

            // 8. Debit Cards Seeding (1-20)
            var debitCards = new List<EDebitCard>();
            for (int i = 1; i <= 20; i++)
            {
                debitCards.Add(new EDebitCard
                {
                    CardId = i,
                    CustomerId = i,
                    CardNumber = $"468500000000{i:D4}",
                    ExpiryDate = "12/29",
                    CVV = $"{200 + i}",
                    Balance = 100000,
                    IBAN = $"TR1100062000000000010000{i:D2}",
                    IsBlocked = false
                });
            }
            modelBuilder.Entity<EDebitCard>().HasData(debitCards);

            // 9. Users Seeding
            // Şifreler: admin123, karar123, analist123
            modelBuilder.Entity<EUser>().HasData(
                new EUser { UserId = 1, Username = "admin", Mail = "admin@fraudguard.com",
                    PasswordUnderSHA256 = "JAvlGPq9JyTdtvBO6x2llnRI1+gxwIyPqCKAn3THIKk=",
                    Role = UserRoleEnum.Admin },
                new EUser { UserId = 2, Username = "karar", Mail = "karar@fraudguard.com",
                    PasswordUnderSHA256 = "ycF0b3KW1cO5eyilr8tdOr8fCd508lL1nE30Wjv8rqk=",
                    Role = UserRoleEnum.DecisionMaker },
                new EUser { UserId = 3, Username = "analist", Mail = "analist@fraudguard.com",
                    PasswordUnderSHA256 = "SeHF2O8QydoPiBO+vMacsLVAAg4yC3Om6zrH6r4F8HY=",
                    Role = UserRoleEnum.Analyst }
            );

            // 10. Transactions Seeding (For history-based rules setup)
            var baseTime = new DateTime(2026, 7, 17, 10, 0, 0);
            modelBuilder.Entity<ETransaction>().HasData(
                // For Card 9 (Musteri 9) - Prior foreign transaction to allow Currency Mismatch without Cross Border
                new ETransaction
                {
                    TransactionId = 1,
                    CreditCardId = 9,
                    DebitCardId = null,
                    TransactionTypeId = 1, // Sale
                    PaymentTypeId = 1, // CreditCard
                    ChannelTypeId = 2, // VirtualPOS
                    Currency = "EUR",
                    Amount = 100,
                    TransactionDate = baseTime.AddHours(-2),
                    Location = "Paris Store",
                    Country = "Fransa",
                    MerchantCategory = "Giyim",
                    Status = "Approved"
                },
                // For Card 12 (Musteri 12) - Prior virtual POS load to trigger Wallet Cash-out
                new ETransaction
                {
                    TransactionId = 2,
                    CreditCardId = null,
                    DebitCardId = 12,
                    SenderIBAN = null,
                    ReceiverIBAN = "TR110006200000000001000012",
                    ReceiverName = "Musteri12",
                    TransactionTypeId = 1, // Sale (Load)
                    PaymentTypeId = 1, // CreditCard
                    ChannelTypeId = 2, // VirtualPOS
                    Currency = "TRY",
                    Amount = 10000,
                    TransactionDate = baseTime.AddMinutes(-5),
                    Location = "Bakiye Yukleme",
                    Country = "Türkiye",
                    MerchantCategory = "Finans",
                    Status = "Approved"
                },
                // For Card 13 (Musteri 13) - Prior deposits from 2 different senders to trigger Multi-Source Funding
                new ETransaction
                {
                    TransactionId = 3,
                    CreditCardId = null,
                    DebitCardId = 13,
                    SenderIBAN = "TR110006200000000001000002",
                    ReceiverIBAN = "TR110006200000000001000013",
                    ReceiverName = "Musteri13",
                    TransactionTypeId = 4, // Transfer (Para Gönderimi)
                    PaymentTypeId = 4, // EFT
                    ChannelTypeId = 4, // Mobile
                    Currency = "TRY",
                    Amount = 5000,
                    TransactionDate = baseTime.AddMinutes(-10),
                    Location = "Mobil Bankacilik",
                    Country = "Türkiye",
                    MerchantCategory = "Para Transferi",
                    Status = "Approved"
                },
                new ETransaction
                {
                    TransactionId = 4,
                    CreditCardId = null,
                    DebitCardId = 13,
                    SenderIBAN = "TR110006200000000001000003",
                    ReceiverIBAN = "TR110006200000000001000013",
                    ReceiverName = "Musteri13",
                    TransactionTypeId = 4, // Transfer (Para Gönderimi)
                    PaymentTypeId = 4, // EFT
                    ChannelTypeId = 4, // Mobile
                    Currency = "TRY",
                    Amount = 5000,
                    TransactionDate = baseTime.AddMinutes(-8),
                    Location = "Mobil Bankacilik",
                    Country = "Türkiye",
                    MerchantCategory = "Para Transferi",
                    Status = "Approved"
                },
                // For Card 19 (Musteri 19) - Prior deposits from 3 different senders to trigger Multi-Sender
                new ETransaction
                {
                    TransactionId = 5,
                    CreditCardId = null,
                    DebitCardId = 19,
                    SenderIBAN = "TR110006200000000001000002",
                    ReceiverIBAN = "TR110006200000000001000019",
                    ReceiverName = "Musteri19",
                    TransactionTypeId = 4, // Transfer (Para Gönderimi)
                    PaymentTypeId = 4, // EFT
                    ChannelTypeId = 4, // Mobile
                    Currency = "TRY",
                    Amount = 1000,
                    TransactionDate = baseTime.AddMinutes(-15),
                    Location = "Mobil Bankacilik",
                    Country = "Türkiye",
                    MerchantCategory = "Para Transferi",
                    Status = "Approved"
                },
                new ETransaction
                {
                    TransactionId = 6,
                    CreditCardId = null,
                    DebitCardId = 19,
                    SenderIBAN = "TR110006200000000001000003",
                    ReceiverIBAN = "TR110006200000000001000019",
                    ReceiverName = "Musteri19",
                    TransactionTypeId = 4, // Transfer (Para Gönderimi)
                    PaymentTypeId = 4, // EFT
                    ChannelTypeId = 4, // Mobile
                    Currency = "TRY",
                    Amount = 1000,
                    TransactionDate = baseTime.AddMinutes(-12),
                    Location = "Mobil Bankacilik",
                    Country = "Türkiye",
                    MerchantCategory = "Para Transferi",
                    Status = "Approved"
                },
                new ETransaction
                {
                    TransactionId = 7,
                    CreditCardId = null,
                    DebitCardId = 19,
                    SenderIBAN = "TR110006200000000001000004",
                    ReceiverIBAN = "TR110006200000000001000019",
                    ReceiverName = "Musteri19",
                    TransactionTypeId = 4, // Transfer (Para Gönderimi)
                    PaymentTypeId = 4, // EFT
                    ChannelTypeId = 4, // Mobile
                    Currency = "TRY",
                    Amount = 1000,
                    TransactionDate = baseTime.AddMinutes(-10),
                    Location = "Mobil Bankacilik",
                    Country = "Türkiye",
                    MerchantCategory = "Para Transferi",
                    Status = "Approved"
                },
                // For Card 10 (Musteri 10) - Prior approved sale transactions so they can issue consecutive refunds
                new ETransaction
                {
                    TransactionId = 8,
                    CreditCardId = 10,
                    DebitCardId = null,
                    TransactionTypeId = 1, // Sale
                    PaymentTypeId = 1, // CreditCard
                    ChannelTypeId = 2, // VirtualPOS
                    Currency = "TRY",
                    Amount = 100,
                    TransactionDate = baseTime.AddHours(-3),
                    Location = "Sanal POS",
                    Country = "Türkiye",
                    MerchantCategory = "Giyim",
                    Status = "Approved"
                },
                new ETransaction
                {
                    TransactionId = 9,
                    CreditCardId = 10,
                    DebitCardId = null,
                    TransactionTypeId = 1, // Sale
                    PaymentTypeId = 1, // CreditCard
                    ChannelTypeId = 2, // VirtualPOS
                    Currency = "TRY",
                    Amount = 100,
                    TransactionDate = baseTime.AddHours(-2),
                    Location = "Sanal POS",
                    Country = "Türkiye",
                    MerchantCategory = "Giyim",
                    Status = "Approved"
                },
                new ETransaction
                {
                    TransactionId = 10,
                    CreditCardId = 10,
                    DebitCardId = null,
                    TransactionTypeId = 1, // Sale
                    PaymentTypeId = 1, // CreditCard
                    ChannelTypeId = 2, // VirtualPOS
                    Currency = "TRY",
                    Amount = 100,
                    TransactionDate = baseTime.AddHours(-1),
                    Location = "Sanal POS",
                    Country = "Türkiye",
                    MerchantCategory = "Giyim",
                    Status = "Approved"
                },
                new ETransaction
                {
                    TransactionId = 11,
                    CreditCardId = 10,
                    DebitCardId = null,
                    TransactionTypeId = 1, // Sale
                    PaymentTypeId = 1, // CreditCard
                    ChannelTypeId = 2, // VirtualPOS
                    Currency = "TRY",
                    Amount = 100,
                    TransactionDate = baseTime.AddHours(-1),
                    Location = "Sanal POS",
                    Country = "Türkiye",
                    MerchantCategory = "Giyim",
                    Status = "Approved"
                },
                new ETransaction
                {
                    TransactionId = 12,
                    CreditCardId = 10,
                    DebitCardId = null,
                    TransactionTypeId = 1, // Sale
                    PaymentTypeId = 1, // CreditCard
                    ChannelTypeId = 2, // VirtualPOS
                    Currency = "TRY",
                    Amount = 100,
                    TransactionDate = baseTime.AddHours(-1),
                    Location = "Sanal POS",
                    Country = "Türkiye",
                    MerchantCategory = "Giyim",
                    Status = "Approved"
                }
            );
        }
    }
}