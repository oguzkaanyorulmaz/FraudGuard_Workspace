using FraudGuard.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FraudGuard.Infrastructure.Persistence.EntityConfigurations
{
    public class CustomerConfiguration : IEntityTypeConfiguration<ECustomer>
    {
        public void Configure(EntityTypeBuilder<ECustomer> builder)
        {
            builder.HasKey(c => c.CustomerId);
            builder.Property(c => c.FirstName).IsRequired().HasMaxLength(50);
            builder.Property(c => c.LastName).IsRequired().HasMaxLength(50);
            
            builder.Property(c => c.IdentityNumber).IsRequired().HasMaxLength(11);
            builder.HasIndex(c => c.IdentityNumber).IsUnique(); 
        }
    }
}