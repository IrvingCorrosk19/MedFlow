using MedFlow.Domain.Entities;
using MedFlow.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MedFlow.Infrastructure.Persistence.Configurations;

public class TenantSubscriptionConfiguration : IEntityTypeConfiguration<TenantSubscription>
{
    public void Configure(EntityTypeBuilder<TenantSubscription> builder)
    {
        builder.ToTable("TenantSubscriptions");

        builder.HasIndex(ts => ts.TenantId);
        builder.HasIndex(ts => ts.SubscriptionPlanId);
        builder.HasIndex(ts => ts.Status);
        builder.HasIndex(ts => ts.TrialEndDate);
        builder.HasIndex(ts => ts.NextBillingDate);
        builder.HasIndex(ts => new { ts.TenantId, ts.Status });

        builder.Property(ts => ts.Notes).HasMaxLength(2000);
        builder.Property(ts => ts.ExternalSubscriptionId).HasMaxLength(256);
        builder.Property(ts => ts.ExternalPlanId).HasMaxLength(256);
        builder.Property(ts => ts.ExternalPriceId).HasMaxLength(256);
        builder.Property(ts => ts.ExternalProductId).HasMaxLength(256);

        builder.HasOne(ts => ts.Tenant)
            .WithMany(t => t.Subscriptions)
            .HasForeignKey(ts => ts.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(ts => ts.SubscriptionPlan)
            .WithMany(p => p.TenantSubscriptions)
            .HasForeignKey(ts => ts.SubscriptionPlanId)
            .OnDelete(DeleteBehavior.Restrict);

        // Un solo registro "operativo" por tenant (Trial, Active, PastDue).
        builder.HasIndex(ts => ts.TenantId)
            .IsUnique()
            .HasFilter($"\"Status\" IN ({(int)SubscriptionStatus.Trial}, {(int)SubscriptionStatus.Active}, {(int)SubscriptionStatus.PastDue})");
    }
}
