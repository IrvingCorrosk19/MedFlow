using MedFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MedFlow.Infrastructure.Persistence.Configurations;

public class CashMovementConfiguration : IEntityTypeConfiguration<CashMovement>
{
    public void Configure(EntityTypeBuilder<CashMovement> builder)
    {
        builder.ToTable("CashMovements");

        builder.Property(m => m.Amount).HasPrecision(18, 2);
        builder.Property(m => m.Description).HasMaxLength(1000);
        builder.Property(m => m.CreatedByUserId).HasMaxLength(450).IsRequired();

        builder.HasOne(m => m.RelatedPayment)
            .WithMany(p => p.CashMovements)
            .HasForeignKey(m => m.RelatedPaymentId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Property(m => m.TenantId).IsRequired();
        builder.HasIndex(m => m.TenantId);
        builder.HasIndex(m => new { m.TenantId, m.MovementDate });

        builder.HasOne(m => m.Tenant)
            .WithMany()
            .HasForeignKey(m => m.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(m => m.MovementDate);
        builder.HasIndex(m => m.MovementType);
    }
}
