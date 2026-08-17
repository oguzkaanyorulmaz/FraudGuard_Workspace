using FraudGuard.Domain.Common.Enums;
using FraudGuard.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FraudGuard.Infrastructure.Persistence.EntityConfigurations
{
    public class CreditCardTransactionConfiguration : IEntityTypeConfiguration<ECreditCardTransaction>
    {
        public void Configure(EntityTypeBuilder<ECreditCardTransaction> builder)
        {
            builder.HasKey(t => t.TransactionId);
            builder.Property(t => t.RRN).IsRequired().HasMaxLength(12);
            builder.Property(t => t.Amount).HasColumnType("decimal(18,2)");
            builder.Property(t => t.Currency).IsRequired().HasMaxLength(3).HasDefaultValue("TRY");
            builder.Property(t => t.Location).HasMaxLength(100);
            builder.Property(t => t.Country).HasMaxLength(50).HasDefaultValue("Türkiye");
            builder.Property(t => t.MerchantCategory).HasMaxLength(50).HasDefaultValue("Diğer");
            builder.Property(t => t.Status).IsRequired().HasMaxLength(20);
            builder.Property(t => t.DeclineReason).HasMaxLength(250);
            builder.Property(t => t.FraudReason).HasMaxLength(250);

            // Motorun kararı işlemle birlikte saklanır; panel yeniden hesaplamaz.
            builder.Property(t => t.RiskScore).HasDefaultValue(0);
            builder.Property(t => t.RiskDecision).HasConversion<int>().HasDefaultValue(RiskDecisionEnum.Normal);

            builder.HasOne(t => t.CreditCard)
                   .WithMany(c => c.Transactions)
                   .HasForeignKey(t => t.CreditCardId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(t => t.TransactionType)
                   .WithMany(tt => tt.CreditCardTransactions)
                   .HasForeignKey(t => t.TransactionTypeId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(t => t.ChannelType)
                   .WithMany(ct => ct.CreditCardTransactions)
                   .HasForeignKey(t => t.ChannelTypeId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
