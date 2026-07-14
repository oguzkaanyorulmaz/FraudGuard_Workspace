using FraudGuard.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace FraudGuard.Infrastructure.Persistence.Contexts
{
    public class FraudGuardDbContext : DbContext
    {
        public FraudGuardDbContext(DbContextOptions<FraudGuardDbContext> options) : base(options) { }

        public DbSet<ECustomer> Customers { get; set; }
        public DbSet<ECreditCard> CreditCards { get; set; }
        public DbSet<ETransaction> Transactions { get; set; }
        
        public DbSet<ETransactionType> TransactionTypes { get; set; } 
        
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
                new ETransactionType { TransactionTypeId = 3, TypeCode = "Void", Description = "İptal İşlemi" }
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
                new EFraudRule { RuleId = 9, RuleCode = "CURRENCY_MISMATCH", RuleName = "Para Birimi Sapması", Description = "Müşterinin kart geçmişinde bulunmayan bir para birimiyle işlem denemesi", IsActive = true }
            );

            // 3. Block Reasons Seeding
            modelBuilder.Entity<EBlockReason>().HasData(
                new EBlockReason { ReasonId = 1, ReasonCode = "Stolen", Description = "Çalıntı" },
                new EBlockReason { ReasonId = 2, ReasonCode = "Fraud", Description = "Dolandırıcılık Şüphesi" },
                new EBlockReason { ReasonId = 3, ReasonCode = "Lost", Description = "Kayıp" }
            );
        }

    }
}