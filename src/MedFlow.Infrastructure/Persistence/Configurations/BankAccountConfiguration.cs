using MedFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MedFlow.Infrastructure.Persistence.Configurations;

public class BankAccountConfiguration : IEntityTypeConfiguration<BankAccount>
{
    public void Configure(EntityTypeBuilder<BankAccount> builder)
    {
        builder.ToTable("BankAccounts");

        builder.Property(b => b.Name).HasMaxLength(150).IsRequired();
        builder.Property(b => b.BankName).HasMaxLength(150).IsRequired();
        builder.Property(b => b.AccountNumber).HasMaxLength(50);
        builder.Property(b => b.Currency).HasMaxLength(10).HasDefaultValue("USD");
        builder.Property(b => b.OpeningBalance).HasPrecision(18, 2);

        builder.HasOne(b => b.Account)
            .WithMany()
            .HasForeignKey(b => b.AccountId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Property(b => b.TenantId).IsRequired();
        builder.HasIndex(b => b.TenantId);

        builder.HasOne(b => b.Tenant)
            .WithMany()
            .HasForeignKey(b => b.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
