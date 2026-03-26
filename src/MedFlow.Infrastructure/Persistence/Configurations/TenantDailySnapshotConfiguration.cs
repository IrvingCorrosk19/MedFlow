using MedFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MedFlow.Infrastructure.Persistence.Configurations;

public class TenantDailySnapshotConfiguration : IEntityTypeConfiguration<TenantDailySnapshot>
{
    public void Configure(EntityTypeBuilder<TenantDailySnapshot> builder)
    {
        builder.ToTable("TenantDailySnapshots");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.TenantId, x.SnapshotDate }).IsUnique();
        builder.HasIndex(x => x.SnapshotDate);
        builder.Property(x => x.RevenueCollected).HasPrecision(18, 2);
        builder.Property(x => x.BalanceDueTotal).HasPrecision(18, 2);
        builder.Property(x => x.RevenueLast7).HasPrecision(18, 2);
        builder.Property(x => x.RevenueLast30).HasPrecision(18, 2);
        builder.Property(x => x.TotalInvoicedAmount).HasPrecision(18, 2);
    }
}
