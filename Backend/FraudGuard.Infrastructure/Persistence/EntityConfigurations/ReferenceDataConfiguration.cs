using FraudGuard.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FraudGuard.Infrastructure.Persistence.EntityConfigurations
{
    public class BinRangeConfiguration : IEntityTypeConfiguration<EBinRange>
    {
        public void Configure(EntityTypeBuilder<EBinRange> builder)
        {
            builder.ToTable("BinRanges");
            builder.HasKey(b => b.BinPrefix);

            builder.Property(b => b.BinPrefix).HasMaxLength(6).IsRequired();
            builder.Property(b => b.CountryCode).HasMaxLength(2).IsRequired();
            builder.Property(b => b.Scheme).HasMaxLength(30).IsRequired();
            builder.Property(b => b.BankName).HasMaxLength(120);

            builder.HasIndex(b => b.IsActive);
        }
    }

    public class ReferenceListEntryConfiguration : IEntityTypeConfiguration<EReferenceListEntry>
    {
        public void Configure(EntityTypeBuilder<EReferenceListEntry> builder)
        {
            builder.ToTable("ReferenceListEntries");
            builder.HasKey(e => e.EntryId);

            builder.Property(e => e.ListType).HasMaxLength(40).IsRequired();
            builder.Property(e => e.Value).HasMaxLength(60).IsRequired();
            builder.Property(e => e.Description).HasMaxLength(200);

            builder.HasIndex(e => new { e.ListType, e.Value }).IsUnique();
        }
    }
}
