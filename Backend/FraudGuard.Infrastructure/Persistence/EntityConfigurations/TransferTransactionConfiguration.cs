using FraudGuard.Domain.Common.Enums;
using FraudGuard.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FraudGuard.Infrastructure.Persistence.EntityConfigurations
{
    public class TransferTransactionConfiguration : IEntityTypeConfiguration<ETransferTransaction>
    {
        public void Configure(EntityTypeBuilder<ETransferTransaction> builder)
        {
            builder.HasKey(t => t.TransactionId);
            builder.Property(t => t.RRN).IsRequired().HasMaxLength(12);
            builder.Property(t => t.SenderIBAN).IsRequired().HasMaxLength(34);
            builder.Property(t => t.ReceiverIBAN).IsRequired().HasMaxLength(34);
            builder.Property(t => t.ReceiverName).IsRequired().HasMaxLength(100);
            builder.Property(t => t.Description).HasMaxLength(250);
            builder.Property(t => t.Amount).HasColumnType("decimal(18,2)");
            builder.Property(t => t.Currency).IsRequired().HasMaxLength(3).HasDefaultValue("TRY");
            builder.Property(t => t.Location).HasMaxLength(100);
            builder.Property(t => t.Country).HasMaxLength(50).HasDefaultValue("Türkiye");
            builder.Property(t => t.Status).IsRequired().HasMaxLength(20);
            builder.Property(t => t.DeclineReason).HasMaxLength(250);
            // Gerekçe özeti kural sayısıyla büyür; sınır FraudDecisionResult ile aynı tutulur.
            builder.Property(t => t.FraudReason).HasMaxLength(500);

            // Motorun kararı işlemle birlikte saklanır; panel yeniden hesaplamaz.
            builder.Property(t => t.RiskScore).HasDefaultValue(0);
            builder.Property(t => t.RiskDecision).HasConversion<int>().HasDefaultValue(RiskDecisionEnum.Normal);

            builder.HasOne(t => t.ChannelType)
                   .WithMany(ct => ct.TransferTransactions)
                   .HasForeignKey(t => t.ChannelTypeId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
