using FraudGuard.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FraudGuard.Infrastructure.Persistence.EntityConfigurations
{
    public class RuleCombinationConfiguration : IEntityTypeConfiguration<ERuleCombination>
    {
        public void Configure(EntityTypeBuilder<ERuleCombination> builder)
        {
            builder.HasKey(c => c.CombinationId);

            builder.Property(c => c.CombinationName).IsRequired().HasMaxLength(120);
            builder.Property(c => c.RuleCodes).IsRequired().HasMaxLength(250);
            builder.Property(c => c.FraudType).HasMaxLength(250);

            builder.Property(c => c.BonusScore).IsRequired();
            builder.Property(c => c.Target).IsRequired().HasConversion<int>();

            builder.HasIndex(c => c.IsActive);
        }
    }
}
