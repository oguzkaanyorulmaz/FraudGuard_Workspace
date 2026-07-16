using FraudGuard.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FraudGuard.Infrastructure.Persistence.EntityConfigurations
{
    public class TransactionConfiguration : IEntityTypeConfiguration<ETransaction>
    {
        public void Configure(EntityTypeBuilder<ETransaction> builder)
        {
            builder.HasKey(t => t.TransactionId);
            builder.Property(t => t.Amount).HasColumnType("decimal(18,2)");
            
            builder.Property(t => t.Currency).IsRequired().HasMaxLength(3).HasDefaultValue("TRY");
            
            builder.HasOne(t => t.TransactionType)
                   .WithMany(tt => tt.Transactions)
                   .HasForeignKey(t => t.TransactionTypeId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(t => t.PaymentType)
                   .WithMany(pt => pt.Transactions)
                   .HasForeignKey(t => t.PaymentTypeId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.Property(t => t.Location).HasMaxLength(100);
            builder.Property(t => t.Country).HasMaxLength(50).HasDefaultValue("Türkiye");
            builder.Property(t => t.MerchantCategory).HasMaxLength(50).HasDefaultValue("Diğer");
            builder.Property(t => t.Status).IsRequired().HasMaxLength(20);
            builder.Property(t => t.DeclineReason).HasMaxLength(250);

            builder.HasOne(t => t.CreditCard)
                   .WithMany(c => c.Transactions)
                   .HasForeignKey(t => t.CardId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}