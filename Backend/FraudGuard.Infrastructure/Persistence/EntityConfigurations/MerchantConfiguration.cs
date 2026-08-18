using FraudGuard.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FraudGuard.Infrastructure.Persistence.EntityConfigurations
{
    public class MerchantConfiguration : IEntityTypeConfiguration<EMerchant>
    {
        public void Configure(EntityTypeBuilder<EMerchant> builder)
        {
            builder.HasKey(m => m.MerchantId);

            // Doğal anahtar: identity değil, seed ve istekte okunabilir kod olarak taşınır.
            builder.Property(m => m.MerchantId).HasMaxLength(20).ValueGeneratedNever();

            builder.Property(m => m.MerchantName).IsRequired().HasMaxLength(120);
            builder.Property(m => m.MccCode).IsRequired().HasMaxLength(4);
            builder.Property(m => m.MerchantCategory).IsRequired().HasMaxLength(50);
            builder.Property(m => m.City).HasMaxLength(60);
            builder.Property(m => m.Country).HasMaxLength(60);

            builder.HasIndex(m => m.IsActive);
        }
    }
}
