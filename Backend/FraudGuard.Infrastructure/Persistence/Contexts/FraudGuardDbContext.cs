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
            modelBuilder.Entity<ETransactionCategory>().HasData(
        new ETransactionCategory { CategoryId = 1, CategoryCode = "Sale", Description = "Satış İşlemi", LimitEffect = 1 },
        new ETransactionCategory { CategoryId = 2, CategoryCode = "Refund", Description = "İade İşlemi", LimitEffect = -1 },
        new ETransactionCategory { CategoryId = 3, CategoryCode = "Void", Description = "İptal İşlemi", LimitEffect = 0 }
    );
        }
    }
}