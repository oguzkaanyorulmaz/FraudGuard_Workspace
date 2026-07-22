using FraudGuard.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FraudGuard.Infrastructure.Persistence.EntityConfigurations
{
    public class FraudLogConfiguration : IEntityTypeConfiguration<EFraudLog>
    {
        public void Configure(EntityTypeBuilder<EFraudLog> builder)
        {
            builder.HasKey(f => f.LogId);
            builder.Property(f => f.AdminAction).HasMaxLength(50);

            // Üç bağımsız tabloya bağlanan One-to-One / Nullable ilişkiler
            builder.HasOne(f => f.CreditCardTransaction)
                   .WithOne(t => t.FraudLog)
                   .HasForeignKey<EFraudLog>(f => f.CreditCardTransactionId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(f => f.DebitCardTransaction)
                   .WithOne(t => t.FraudLog)
                   .HasForeignKey<EFraudLog>(f => f.DebitCardTransactionId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(f => f.TransferTransaction)
                   .WithOne(t => t.FraudLog)
                   .HasForeignKey<EFraudLog>(f => f.TransferTransactionId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(f => f.FraudRule)
                   .WithMany()
                   .HasForeignKey(f => f.RuleId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
