using FraudGuard.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FraudGuard.Infrastructure.Persistence.EntityConfigurations
{
    public class FraudRuleConfiguration : IEntityTypeConfiguration<EFraudRule>
    {
        public void Configure(EntityTypeBuilder<EFraudRule> builder)
        {
            builder.HasKey(f => f.RuleId);
            builder.Property(f => f.RuleCode).IsRequired().HasMaxLength(20);
            builder.HasIndex(f => f.RuleCode).IsUnique();
            builder.Property(f => f.RuleName).IsRequired().HasMaxLength(100);
            builder.Property(f => f.Description).HasMaxLength(250);
        }
    }
}