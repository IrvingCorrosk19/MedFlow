using MedFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MedFlow.Infrastructure.Persistence.Configurations;

public class BillingInvoiceItemConfiguration : IEntityTypeConfiguration<BillingInvoiceItem>
{
    public void Configure(EntityTypeBuilder<BillingInvoiceItem> builder)
    {
        builder.ToTable("BillingInvoiceItems");

        builder.Property(x => x.Description).HasMaxLength(500).IsRequired();
        builder.Property(x => x.UnitPrice).HasPrecision(18, 2);
        builder.Property(x => x.DiscountAmount).HasPrecision(18, 2);
        builder.Property(x => x.TotalLineAmount).HasPrecision(18, 2);

        builder.HasOne(x => x.BillingInvoice)
            .WithMany(i => i.Items)
            .HasForeignKey(x => x.BillingInvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(x => x.TenantId).IsRequired();
        builder.HasIndex(x => x.TenantId);

        builder.HasOne(x => x.Tenant)
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
