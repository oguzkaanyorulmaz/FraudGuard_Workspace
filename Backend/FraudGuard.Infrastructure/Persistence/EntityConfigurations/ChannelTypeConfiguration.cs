using FraudGuard.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FraudGuard.Infrastructure.Persistence.EntityConfigurations
{
    public class ChannelTypeConfiguration : IEntityTypeConfiguration<EChannelType>
    {
        public void Configure(EntityTypeBuilder<EChannelType> builder)
        {
            builder.HasKey(c => c.ChannelTypeId);
            builder.Property(c => c.ChannelCode).IsRequired().HasMaxLength(20);
            builder.Property(c => c.Description).HasMaxLength(100);
        }
    }
}
