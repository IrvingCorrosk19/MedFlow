using MedFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MedFlow.Infrastructure.Persistence.Configurations;

public class TenantBillingProfileConfiguration : IEntityTypeConfiguration<TenantBillingProfile>
{
    public void Configure(EntityTypeBuilder<TenantBillingProfile> builder)
    {
        builder.ToTable("TenantBillingProfiles");

        builder.HasIndex(b => b.TenantId).IsUnique();
        builder.HasIndex(b => b.ExternalCustomerId).HasFilter("\"ExternalCustomerId\" IS NOT NULL");

        builder.Property(b => b.ExternalCustomerId).HasMaxLength(256);
        builder.Property(b => b.BillingEmail).HasMaxLength(256);
        builder.Property(b => b.LegalName).HasMaxLength(300);
        builder.Property(b => b.TaxId).HasMaxLength(64);
        builder.Property(b => b.Country).HasMaxLength(2);
        builder.Property(b => b.StateProvince).HasMaxLength(100);
        builder.Property(b => b.City).HasMaxLength(100);
        builder.Property(b => b.AddressLine1).HasMaxLength(300);
        builder.Property(b => b.AddressLine2).HasMaxLength(300);
        builder.Property(b => b.PostalCode).HasMaxLength(20);
        builder.Property(b => b.PreferredCurrency).HasMaxLength(8);

        builder.HasOne(b => b.Tenant)
            .WithOne(t => t.BillingProfile)
            .HasForeignKey<TenantBillingProfile>(b => b.TenantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
