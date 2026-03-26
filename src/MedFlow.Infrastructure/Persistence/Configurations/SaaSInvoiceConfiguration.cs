using MedFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MedFlow.Infrastructure.Persistence.Configurations;

public class SaaSInvoiceConfiguration : IEntityTypeConfiguration<SaaSInvoice>
{
    public void Configure(EntityTypeBuilder<SaaSInvoice> builder)
    {
        builder.ToTable("SaaSInvoices");

        builder.HasIndex(i => i.TenantId);
        builder.HasIndex(i => i.TenantSubscriptionId);
        builder.HasIndex(i => i.InvoiceNumber).IsUnique();
        builder.HasIndex(i => i.ProviderInvoiceId).HasFilter("\"ProviderInvoiceId\" IS NOT NULL");

        builder.Property(i => i.InvoiceNumber).HasMaxLength(64);
        builder.Property(i => i.Currency).HasMaxLength(8);
        builder.Property(i => i.ProviderInvoiceId).HasMaxLength(256);
        builder.Property(i => i.InvoiceUrl).HasMaxLength(1024);
        builder.Property(i => i.PdfUrl).HasMaxLength(1024);
        builder.Property(i => i.Notes).HasMaxLength(2000);

        builder.HasOne(i => i.Tenant)
            .WithMany()
            .HasForeignKey(i => i.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.TenantSubscription)
            .WithMany()
            .HasForeignKey(i => i.TenantSubscriptionId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
