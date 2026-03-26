using MedFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MedFlow.Infrastructure.Persistence.Configurations;

public class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("Tenants");

        builder.HasIndex(t => t.Code).IsUnique();

        builder.Property(t => t.Name).HasMaxLength(200).IsRequired();
        builder.Property(t => t.Code).HasMaxLength(80).IsRequired();
        builder.Property(t => t.LegalName).HasMaxLength(300);
        builder.Property(t => t.TaxId).HasMaxLength(50);
        builder.Property(t => t.Address).HasMaxLength(500);
        builder.Property(t => t.Phone).HasMaxLength(30);
        builder.Property(t => t.Email).HasMaxLength(256);
        builder.Property(t => t.LogoUrl).HasMaxLength(500);
        builder.Property(t => t.PrimaryColor).HasMaxLength(32);
        builder.Property(t => t.SecondaryColor).HasMaxLength(32);
        builder.Property(t => t.SuspensionReason).HasMaxLength(1000);

        builder.HasIndex(t => t.CommercialStatus);
        builder.HasIndex(t => t.IsSuspended);
        builder.HasIndex(t => t.CurrentSubscriptionId);

        builder.HasOne(t => t.CurrentSubscription)
            .WithOne()
            .HasForeignKey<Tenant>(t => t.CurrentSubscriptionId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
