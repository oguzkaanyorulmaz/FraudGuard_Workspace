using FraudGuard.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FraudGuard.Infrastructure.Persistence.EntityConfigurations
{
    public class DebitCardConfiguration : IEntityTypeConfiguration<EDebitCard>
    {
        public void Configure(EntityTypeBuilder<EDebitCard> builder)
        {
            builder.HasKey(d => d.CardId);
            builder.Property(d => d.CardNumber).IsRequired().HasMaxLength(16);
            builder.HasIndex(d => d.CardNumber).IsUnique();
            builder.Property(d => d.IBAN).IsRequired().HasMaxLength(34);
            builder.HasIndex(d => d.IBAN).IsUnique();
            builder.Property(d => d.Balance).HasColumnType("decimal(18,2)");

            builder.HasOne(d => d.Customer)
                   .WithMany()
                   .HasForeignKey(d => d.CustomerId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(d => d.BlockReason)
                   .WithMany()
                   .HasForeignKey(d => d.BlockReasonId)
                   .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
