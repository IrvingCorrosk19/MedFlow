using MedFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MedFlow.Infrastructure.Persistence.Configurations;

public class TenantSubscriptionHistoryConfiguration : IEntityTypeConfiguration<TenantSubscriptionHistory>
{
    public void Configure(EntityTypeBuilder<TenantSubscriptionHistory> builder)
    {
        builder.ToTable("TenantSubscriptionHistories");

        builder.HasIndex(h => h.TenantId);
        builder.HasIndex(h => h.CreatedAt);

        builder.Property(h => h.ChangeReason).HasMaxLength(1000).IsRequired();

        builder.HasOne(h => h.Tenant)
            .WithMany(t => t.SubscriptionHistories)
            .HasForeignKey(h => h.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(h => h.PreviousPlan)
            .WithMany()
            .HasForeignKey(h => h.PreviousPlanId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(h => h.NewPlan)
            .WithMany()
            .HasForeignKey(h => h.NewPlanId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
