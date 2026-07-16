using FraudGuard.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FraudGuard.Infrastructure.Persistence.EntityConfigurations
{
    public class UserConfiguration : IEntityTypeConfiguration<EUser>
    {
        public void Configure(EntityTypeBuilder<EUser> builder)
        {
            builder.ToTable("Users");
            builder.HasKey(u => u.UserId);
            builder.HasIndex(u => u.Username).IsUnique();
            builder.Property(u => u.Username).IsRequired().HasMaxLength(50);
            builder.Property(u => u.Mail).IsRequired().HasMaxLength(100);
            builder.Property(u => u.PasswordUnderSHA256).IsRequired().HasMaxLength(100);
            builder.Property(u => u.Role).IsRequired();
        }
    }
}
