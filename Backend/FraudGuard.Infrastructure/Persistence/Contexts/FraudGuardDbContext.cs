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
                new ETransactionType { TransactionTypeId = 3, TypeCode = "Deposit", Description = "Para Yatırma" },
                new ETransactionType { TransactionTypeId = 4, TypeCode = "CardPayment", Description = "Kredi Kartı Borç Ödeme" }
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
                new EFraudRule { RuleId = 20, RuleCode = "RECEIVER_BALANCE_ANOMALY", RuleName = "Katır Hesap Bakiye Sapması", Description = "Pasif hesaba ani bakiye gelip 1 saatte nakit çekilmeye çalışılması", IsActive = true },
                new EFraudRule { RuleId = 22, RuleCode = "HIGH_VALUE_REFUND_VOID", RuleName = "Yüksek Tutarlı İade Kuralı", Description = "Tek seferde 10.000 TL ve üzerinde İade (Refund) işlemi yapılması", IsActive = true },
                new EFraudRule { RuleId = 23, RuleCode = "DEPOSIT_AND_RUN", RuleName = "Yatır ve Kaç Kuralı", Description = "Son 10 dakika içinde hesaba ATM'den para yatırıldıktan hemen sonra bu tutarın %90'ından fazlasının harcanmak istenmesi", IsActive = true },
                new EFraudRule { RuleId = 24, RuleCode = "DEPOSIT_LIMIT_AVOIDANCE", RuleName = "Yapılandırılmış Aklama (Deposit Limit Avoidance)", Description = "Son 24 saat içinde 3 veya daha fazla farklı ATM'den toplamda 40.000 TL ve üzeri para yatırma denemesi", IsActive = true },
                new EFraudRule { RuleId = 25, RuleCode = "ANOMALOUS_DEPOSIT_TIME", RuleName = "Gece Yarısı Nakit Akışı", Description = "Gece geç saatte (23:00-06:00) ATM'den 10.000 TL ve üzeri nakit para yatırılması", IsActive = true }
            );

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