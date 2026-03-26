using MedFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MedFlow.Infrastructure.Persistence.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payments");

        builder.Property(p => p.Amount).HasPrecision(18, 2);
        builder.Property(p => p.ReferenceNumber).HasMaxLength(100);
        builder.Property(p => p.Notes).HasMaxLength(2000);
        builder.Property(p => p.ReceivedByUserId).HasMaxLength(450);

        builder.HasOne(p => p.BillingInvoice)
            .WithMany(i => i.Payments)
            .HasForeignKey(p => p.BillingInvoiceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Patient)
            .WithMany(pt => pt.Payments)
            .HasForeignKey(p => p.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(p => p.TenantId).IsRequired();
        builder.HasIndex(p => p.TenantId);
        builder.HasIndex(p => new { p.TenantId, p.PaymentDate });

        builder.HasOne(p => p.Tenant)
            .WithMany()
            .HasForeignKey(p => p.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(p => p.PaymentDate);
        builder.HasIndex(p => new { p.Status, p.PaymentDate });
        builder.HasIndex(p => p.BillingInvoiceId);
        builder.HasIndex(p => p.PatientId);
    }
}
