using FraudGuard.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FraudGuard.Infrastructure.Persistence.EntityConfigurations
{
    public class PaymentTypeConfiguration : IEntityTypeConfiguration<EPaymentType>
    {
        public void Configure(EntityTypeBuilder<EPaymentType> builder)
        {
            builder.HasKey(p => p.PaymentTypeId);
            
            builder.Property(p => p.TypeCode).IsRequired().HasMaxLength(20);
            builder.HasIndex(p => p.TypeCode).IsUnique();
            
            builder.Property(p => p.Description).IsRequired().HasMaxLength(100);
        }
    }
}
