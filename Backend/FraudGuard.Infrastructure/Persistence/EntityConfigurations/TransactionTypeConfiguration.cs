using FraudGuard.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FraudGuard.Infrastructure.Persistence.EntityConfigurations
{
    public class TransactionTypeConfiguration : IEntityTypeConfiguration<ETransactionType>
    {
        public void Configure(EntityTypeBuilder<ETransactionType> builder)
        {
            builder.HasKey(t => t.TransactionTypeId);
            
            builder.Property(t => t.TypeCode).IsRequired().HasMaxLength(20);
            builder.HasIndex(t => t.TypeCode).IsUnique();
            
            builder.Property(t => t.Description).IsRequired().HasMaxLength(100);
        }
    }
}