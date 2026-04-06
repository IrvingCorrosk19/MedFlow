using MedFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MedFlow.Infrastructure.Persistence.Configurations;

public class FiscalPeriodConfiguration : IEntityTypeConfiguration<FiscalPeriod>
{
    public void Configure(EntityTypeBuilder<FiscalPeriod> builder)
    {
        builder.ToTable("FiscalPeriods");

        builder.Property(f => f.Name).HasMaxLength(100).IsRequired();
        builder.Property(f => f.ClosedByUserId).HasMaxLength(450);

        builder.HasIndex(f => new { f.TenantId, f.Year, f.Month }).IsUnique();

        builder.Property(f => f.TenantId).IsRequired();
        builder.HasIndex(f => f.TenantId);
        builder.HasIndex(f => new { f.TenantId, f.Status });

        builder.HasOne(f => f.Tenant)
            .WithMany()
            .HasForeignKey(f => f.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
