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

            builder.Property(f => f.RuleCode).IsRequired().HasMaxLength(50);
            builder.HasIndex(f => f.RuleCode).IsUnique();

            builder.Property(f => f.RuleName).IsRequired().HasMaxLength(100);
            builder.Property(f => f.Description).HasMaxLength(250);

            // Dinamik kural ifadesi. Boş olması kuralın kod tabanlı olduğu anlamına gelir.
            builder.Property(f => f.Expression).HasMaxLength(1000);

            builder.Property(f => f.Score).IsRequired();
            builder.Property(f => f.IsCritical).IsRequired().HasDefaultValue(false);
            builder.Property(f => f.Target).IsRequired().HasConversion<int>();
            builder.Property(f => f.Category).IsRequired().HasConversion<int>();

            // Hesaplanan property; kolon olarak yazılmaz.
            builder.Ignore(f => f.IsExpressionBased);

            // Motor her işlemde yalnızca aktif kuralları çeker.
            builder.HasIndex(f => f.IsActive);
        }
    }
}
