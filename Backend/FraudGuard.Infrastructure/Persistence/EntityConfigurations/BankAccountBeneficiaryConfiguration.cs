using FraudGuard.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FraudGuard.Infrastructure.Persistence.EntityConfigurations
{
    public class BankAccountBeneficiaryConfiguration : IEntityTypeConfiguration<EBankAccountBeneficiary>
    {
        public void Configure(EntityTypeBuilder<EBankAccountBeneficiary> builder)
        {
            builder.HasKey(b => b.BeneficiaryId);
            
            builder.Property(b => b.ReceiverIBAN).IsRequired().HasMaxLength(34);
            builder.Property(b => b.ReceiverName).IsRequired().HasMaxLength(100);

            builder.HasOne(b => b.Customer)
                   .WithMany()
                   .HasForeignKey(b => b.CustomerId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
