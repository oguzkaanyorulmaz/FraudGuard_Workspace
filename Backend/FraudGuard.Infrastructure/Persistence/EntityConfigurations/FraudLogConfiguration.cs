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

            builder.HasOne(f => f.Transaction)
                   .WithOne(t => t.FraudLog)
                   .HasForeignKey<EFraudLog>(f => f.TransactionId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(f => f.FraudRule)
                   .WithMany()
                   .HasForeignKey(f => f.RuleId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}