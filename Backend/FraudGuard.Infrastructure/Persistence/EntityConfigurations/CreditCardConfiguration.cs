using FraudGuard.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FraudGuard.Infrastructure.Persistence.EntityConfigurations
{
    public class CreditCardConfiguration : IEntityTypeConfiguration<ECreditCard>
    {
        public void Configure(EntityTypeBuilder<ECreditCard> builder)
        {
            builder.HasKey(c => c.CardId);
            
            builder.Property(c => c.CardNumber).IsRequired().HasMaxLength(16);
            builder.HasIndex(c => c.CardNumber).IsUnique();
            
            builder.Property(c => c.ExpiryDate).IsRequired().HasMaxLength(5);
            builder.Property(c => c.CVV).IsRequired().HasMaxLength(3);
            
            builder.Property(c => c.CardLimit).HasColumnType("decimal(18,2)");

            builder.HasOne(c => c.Customer)
                   .WithMany()
                   .HasForeignKey(c => c.CustomerId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(c => c.BlockReason)
                   .WithMany()
                   .HasForeignKey(c => c.BlockReasonId)
                   .OnDelete(DeleteBehavior.SetNull);
        }
    }
}