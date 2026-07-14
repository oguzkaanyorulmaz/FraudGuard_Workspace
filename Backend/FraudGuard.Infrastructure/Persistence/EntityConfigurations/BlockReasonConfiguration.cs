using FraudGuard.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FraudGuard.Infrastructure.Persistence.EntityConfigurations
{
    public class BlockReasonConfiguration : IEntityTypeConfiguration<EBlockReason>
    {
        public void Configure(EntityTypeBuilder<EBlockReason> builder)
        {
            builder.HasKey(b => b.ReasonId);
            builder.Property(b => b.ReasonCode).IsRequired().HasMaxLength(20);
            builder.HasIndex(b => b.ReasonCode).IsUnique();
            builder.Property(b => b.Description).HasMaxLength(100);
        }
    }
}