using MedFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MedFlow.Infrastructure.Persistence.Configurations;

public class BillingInvoiceConfiguration : IEntityTypeConfiguration<BillingInvoice>
{
    public void Configure(EntityTypeBuilder<BillingInvoice> builder)
    {
        builder.ToTable("BillingInvoices");

        builder.Property(i => i.InvoiceNumber).HasMaxLength(40).IsRequired();
        builder.HasIndex(i => i.InvoiceNumber).IsUnique();

        builder.Property(i => i.Subtotal).HasPrecision(18, 2);
        builder.Property(i => i.DiscountAmount).HasPrecision(18, 2);
        builder.Property(i => i.TaxAmount).HasPrecision(18, 2);
        builder.Property(i => i.TotalAmount).HasPrecision(18, 2);
        builder.Property(i => i.AmountPaid).HasPrecision(18, 2);
        builder.Property(i => i.BalanceDue).HasPrecision(18, 2);
        builder.Property(i => i.Notes).HasMaxLength(4000);

        builder.HasOne(i => i.Patient)
            .WithMany(p => p.BillingInvoices)
            .HasForeignKey(i => i.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.Doctor)
            .WithMany(d => d.BillingInvoices)
            .HasForeignKey(i => i.DoctorId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(i => i.Appointment)
            .WithMany(a => a.BillingInvoices)
            .HasForeignKey(i => i.AppointmentId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Property(i => i.TenantId).IsRequired();
        builder.HasIndex(i => i.TenantId);
        builder.HasIndex(i => new { i.TenantId, i.IssueDate });

        builder.HasOne(i => i.Tenant)
            .WithMany()
            .HasForeignKey(i => i.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(i => new { i.PatientId, i.IssueDate });
        builder.HasIndex(i => i.Status);
        builder.HasIndex(i => new { i.TenantId, i.Status });
    }
}
