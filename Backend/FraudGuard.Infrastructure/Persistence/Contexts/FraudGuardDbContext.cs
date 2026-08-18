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
        public DbSet<ECreditCardTransaction> CreditCardTransactions { get; set; }
        public DbSet<EDebitCardTransaction> DebitCardTransactions { get; set; }
        public DbSet<ETransferTransaction> TransferTransactions { get; set; }
        public DbSet<EDebitCard> DebitCards { get; set; }
        public DbSet<EChannelType> ChannelTypes { get; set; }
        public DbSet<EBankAccountBeneficiary> BankAccountBeneficiaries { get; set; }
        public DbSet<ETransactionType> TransactionTypes { get; set; } 
        public DbSet<EUser> Users { get; set; }
        public DbSet<EFraudRule> FraudRules { get; set; }
        public DbSet<ERuleCombination> RuleCombinations { get; set; }
        public DbSet<EFraudLog> FraudLogs { get; set; }
        public DbSet<EBlockReason> BlockReasons { get; set; }
        public DbSet<EMerchant> Merchants { get; set; }

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
                new ETransactionType { TransactionTypeId = 3, TypeCode = "Deposit", Description = "Para Yatırma" },
                new ETransactionType { TransactionTypeId = 4, TypeCode = "CardPayment", Description = "Kredi Kartı Borç Ödeme" }
            );


            // 2. Fraud Rules Seeding
            // Kural kataloğu ve kombinasyon tanımları ayrı seed sınıflarında tutulur;
            // yeni dinamik kural eklemek için yalnızca FraudRuleSeedData güncellenir.
            modelBuilder.Entity<EFraudRule>().HasData(
                FraudGuard.Infrastructure.Persistence.SeedData.FraudRuleSeedData.GetRules());

            modelBuilder.Entity<ERuleCombination>().HasData(
                FraudGuard.Infrastructure.Persistence.SeedData.RuleCombinationSeedData.GetCombinations());

            // 2.5. Merchant Seeding
            // İşyeri bazlı sayaçlar (farklı kart sayısı, POS yaşı) bu master veriye dayanır.
            modelBuilder.Entity<EMerchant>().HasData(
                FraudGuard.Infrastructure.Persistence.SeedData.MerchantSeedData.GetMerchants());

            // 3. Block Reasons Seeding
            modelBuilder.Entity<EBlockReason>().HasData(
                new EBlockReason { ReasonId = 1, ReasonCode = "Stolen", Description = "Çalıntı" },
                new EBlockReason { ReasonId = 2, ReasonCode = "Fraud", Description = "Dolandırıcılık Şüphesi" },
                new EBlockReason { ReasonId = 3, ReasonCode = "Lost", Description = "Kayıp" }
            );



            // 5. Customers Seeding (1-45)
            var nationalPlayers = new (string First, string Last)[] {
                ("Arda", "Güler"),
                ("Hakan", "Çalhanoğlu"),
                ("Kenan", "Yıldız"),
                ("Barış Alper", "Yılmaz"),
                ("Kerem", "Aktürkoğlu"),
                ("Ferdi", "Kadıoğlu"),
                ("Merih", "Demiral"),
                ("Abdülkerim", "Bardakcı"),
                ("Uğurcan", "Çakır"),
                ("Orkun", "Kökçü"),
                ("İrfan Can", "Kahveci"),
                ("Semih", "Kılıçsoy"),
                ("Cenk", "Tosun"),
                ("Yusuf", "Yazıcı"),
                ("Zeki", "Çelik")
            };

            var firstNames = new string[] {
                "Ahmet", "Mehmet", "Mustafa", "Ali", "Hüseyin", "Hasan", "İbrahim", "Halil", "Yusuf", "Murat",
                "Ömer", "Zeynep", "Elif", "Merve", "Fatma", "Ayşe", "Emine", "Hatice", "Selin", "Esra",
                "Gökhan", "Hakan", "Serkan", "Volkan", "Burak", "Can", "Cem", "Deniz", "Ege", "Kaan",
                "Onur", "Umut", "Oğuz", "Yiğit", "Berkay", "Büşra", "Dilek", "Gamze", "Seda", "Sibel",
                "Gizem", "Derya", "Hande", "Tuğba", "Pınar"
            };
            var lastNames = new string[] {
                "Yılmaz", "Kaya", "Demir", "Çelik", "Şahin", "Yıldız", "Yıldırım", "Öztürk", "Aydın", "Özdemir",
                "Arslan", "Doğan", "Kılıç", "Aslan", "Çetin", "Kara", "Koç", "Kurt", "Tekin", "Acar",
                "Aksoy", "Polat", "Erdoğan", "Güler", "Şen", "Güneş", "Bulut", "Yalçın", "Altun", "Sarı",
                "Avcı", "Eser", "Çakır", "Uysal", "Kartal", "Karahan", "Yavuz", "Şimşek", "Karaca", "Çakmak",
                "Gök", "Duman", "Bozkurt", "Özcan", "Toprak"
            };

            var customers = new List<ECustomer>();
            int nationalIdx = 0;
            for (int i = 1; i <= 45; i++)
            {
                string firstName;
                string lastName;

                // Her 3 müşteriden birini Milli Takım oyuncusu yapalım
                if (i % 3 == 0 && nationalIdx < nationalPlayers.Length)
                {
                    firstName = nationalPlayers[nationalIdx].First;
                    lastName = nationalPlayers[nationalIdx].Last;
                    nationalIdx++;
                }
                else
                {
                    firstName = firstNames[(i - 1) % firstNames.Length];
                    lastName = lastNames[(i - 1) % lastNames.Length];
                }

                string cleanEmailPrefix = firstName.Replace(" ", "").ToLower(System.Globalization.CultureInfo.InvariantCulture);

                customers.Add(new ECustomer
                {
                    CustomerId = i,
                    FirstName = firstName,
                    LastName = lastName,
                    IdentityNumber = (10000000000 + i).ToString(),
                    PhoneNumber = $"+9055555555{i:D2}",
                    Email = $"{cleanEmailPrefix}{i}@mail.com",
                    CreatedAt = new DateTime(2026, 7, 16)
                });
            }
            modelBuilder.Entity<ECustomer>().HasData(customers);

            // 6. Credit Cards Seeding (1-45)
            var creditCards = new List<ECreditCard>();
            for (int i = 1; i <= 45; i++)
            {
                string prefix = $"55200000000{i:D4}";
                int checkDigit = CalculateLuhnCheckDigit(prefix);
                creditCards.Add(new ECreditCard
                {
                    CardId = i,
                    CustomerId = i,
                    CardNumber = prefix + checkDigit,
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

            // 8. Debit Cards Seeding (1-45)
            var debitCards = new List<EDebitCard>();
            for (int i = 1; i <= 45; i++)
            {
                string prefix = $"46850000000{i:D4}";
                int checkDigit = CalculateLuhnCheckDigit(prefix);
                debitCards.Add(new EDebitCard
                {
                    CardId = i,
                    CustomerId = i,
                    CardNumber = prefix + checkDigit,
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

            // 10. Mock Data Seeding (Tüm eski ETransaction verilerinin 3 yeni tabloya bölünmüş ve RRN atanmış hali)
            var baseTime = new DateTime(2026, 7, 17, 10, 0, 0);

            // Kredi Kartı Simülasyon Verileri
            modelBuilder.Entity<ECreditCardTransaction>().HasData(
                new ECreditCardTransaction
                {
                    TransactionId = 1,
                    RRN = "100000000001",
                    CreditCardId = 9,
                    TransactionTypeId = 1, // Sale
                    ChannelTypeId = 2, // VirtualPOS
                    Currency = "EUR",
                    Amount = 100,
                    TransactionDate = baseTime.AddHours(-2),
                    Location = "Paris Store",
                    Country = "Fransa",
                    MerchantCategory = "Giyim",
                    Status = "Approved"
                },
                new ECreditCardTransaction
                {
                    TransactionId = 8,
                    RRN = "100000000008",
                    CreditCardId = 10,
                    TransactionTypeId = 1, // Sale
                    ChannelTypeId = 2, // VirtualPOS
                    Currency = "TRY",
                    Amount = 100,
                    TransactionDate = baseTime.AddHours(-3),
                    Location = "Sanal POS",
                    Country = "Türkiye",
                    MerchantCategory = "Giyim",
                    Status = "Approved"
                },
                new ECreditCardTransaction
                {
                    TransactionId = 9,
                    RRN = "100000000009",
                    CreditCardId = 10,
                    TransactionTypeId = 1, // Sale
                    ChannelTypeId = 2, // VirtualPOS
                    Currency = "TRY",
                    Amount = 100,
                    TransactionDate = baseTime.AddHours(-2),
                    Location = "Sanal POS",
                    Country = "Türkiye",
                    MerchantCategory = "Giyim",
                    Status = "Approved"
                },
                new ECreditCardTransaction
                {
                    TransactionId = 10,
                    RRN = "100000000010",
                    CreditCardId = 10,
                    TransactionTypeId = 1, // Sale
                    ChannelTypeId = 2, // VirtualPOS
                    Currency = "TRY",
                    Amount = 100,
                    TransactionDate = baseTime.AddHours(-1),
                    Location = "Sanal POS",
                    Country = "Türkiye",
                    MerchantCategory = "Giyim",
                    Status = "Approved"
                },
                new ECreditCardTransaction
                {
                    TransactionId = 11,
                    RRN = "100000000011",
                    CreditCardId = 10,
                    TransactionTypeId = 1, // Sale
                    ChannelTypeId = 2, // VirtualPOS
                    Currency = "TRY",
                    Amount = 100,
                    TransactionDate = baseTime.AddHours(-1),
                    Location = "Sanal POS",
                    Country = "Türkiye",
                    MerchantCategory = "Giyim",
                    Status = "Approved"
                },
                new ECreditCardTransaction
                {
                    TransactionId = 12,
                    RRN = "100000000012",
                    CreditCardId = 10,
                    TransactionTypeId = 1, // Sale
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

            // Banka Kartı Simülasyon Verileri
            modelBuilder.Entity<EDebitCardTransaction>().HasData(
                new EDebitCardTransaction
                {
                    TransactionId = 2,
                    RRN = "200000000002",
                    DebitCardId = 12,
                    TransactionTypeId = 1, // Sale
                    ChannelTypeId = 2, // VirtualPOS
                    Currency = "TRY",
                    Amount = 10000,
                    TransactionDate = baseTime.AddMinutes(-5),
                    Location = "Bakiye Yukleme",
                    Country = "Türkiye",
                    MerchantCategory = "Finans",
                    Status = "Approved"
                }
            );

            // Transfer Simülasyon Verileri
            modelBuilder.Entity<ETransferTransaction>().HasData(
                new ETransferTransaction
                {
                    TransactionId = 3,
                    RRN = "300000000003",
                    SenderIBAN = "TR110006200000000001000002",
                    ReceiverIBAN = "TR110006200000000001000013",
                    ReceiverName = "Musteri13",
                    ChannelTypeId = 4, // Mobile
                    Currency = "TRY",
                    Amount = 5000,
                    TransactionDate = baseTime.AddMinutes(-10),
                    Location = "Mobil Bankacilik",
                    Country = "Türkiye",
                    Status = "Approved"
                },
                new ETransferTransaction
                {
                    TransactionId = 4,
                    RRN = "300000000004",
                    SenderIBAN = "TR110006200000000001000003",
                    ReceiverIBAN = "TR110006200000000001000013",
                    ReceiverName = "Musteri13",
                    ChannelTypeId = 4, // Mobile
                    Currency = "TRY",
                    Amount = 5000,
                    TransactionDate = baseTime.AddMinutes(-8),
                    Location = "Mobil Bankacilik",
                    Country = "Türkiye",
                    Status = "Approved"
                },
                new ETransferTransaction
                {
                    TransactionId = 5,
                    RRN = "300000000005",
                    SenderIBAN = "TR110006200000000001000002",
                    ReceiverIBAN = "TR110006200000000001000019",
                    ReceiverName = "Musteri19",
                    ChannelTypeId = 4, // Mobile
                    Currency = "TRY",
                    Amount = 1000,
                    TransactionDate = baseTime.AddMinutes(-15),
                    Location = "Mobil Bankacilik",
                    Country = "Türkiye",
                    Status = "Approved"
                },
                new ETransferTransaction
                {
                    TransactionId = 6,
                    RRN = "300000000006",
                    SenderIBAN = "TR110006200000000001000003",
                    ReceiverIBAN = "TR110006200000000001000019",
                    ReceiverName = "Musteri19",
                    ChannelTypeId = 4, // Mobile
                    Currency = "TRY",
                    Amount = 1000,
                    TransactionDate = baseTime.AddMinutes(-12),
                    Location = "Mobil Bankacilik",
                    Country = "Türkiye",
                    Status = "Approved"
                },
                new ETransferTransaction
                {
                    TransactionId = 7,
                    RRN = "300000000007",
                    SenderIBAN = "TR110006200000000001000004",
                    ReceiverIBAN = "TR110006200000000001000019",
                    ReceiverName = "Musteri19",
                    ChannelTypeId = 4, // Mobile
                    Currency = "TRY",
                    Amount = 1000,
                    TransactionDate = baseTime.AddMinutes(-10),
                    Location = "Mobil Bankacilik",
                    Country = "Türkiye",
                    Status = "Approved"
                }
            );
        }

        private static int CalculateLuhnCheckDigit(string number)
        {
            int sum = 0;
            bool shouldDouble = true;
            for (int i = number.Length - 1; i >= 0; i--)
            {
                int digit = number[i] - '0';
                if (shouldDouble)
                {
                    digit *= 2;
                    if (digit > 9) digit -= 9;
                }
                sum += digit;
                shouldDouble = !shouldDouble;
            }
            return (10 - (sum % 10)) % 10;
        }
    }
}