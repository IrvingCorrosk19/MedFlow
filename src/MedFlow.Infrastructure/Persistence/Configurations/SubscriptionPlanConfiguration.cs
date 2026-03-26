using MedFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MedFlow.Infrastructure.Persistence.Configurations;

public class SubscriptionPlanConfiguration : IEntityTypeConfiguration<SubscriptionPlan>
{
    public void Configure(EntityTypeBuilder<SubscriptionPlan> builder)
    {
        builder.ToTable("SubscriptionPlans");

        builder.HasIndex(p => p.Code).IsUnique();

        builder.Property(p => p.Name).HasMaxLength(120).IsRequired();
        builder.Property(p => p.Code).HasMaxLength(64).IsRequired();
        builder.Property(p => p.Description).HasMaxLength(2000);
        builder.Property(p => p.Currency).HasMaxLength(8).IsRequired();
        builder.Property(p => p.MonthlyPrice).HasPrecision(18, 2);
        builder.Property(p => p.AnnualPrice).HasPrecision(18, 2);
        builder.Property(p => p.StripePriceIdMonthly).HasMaxLength(256);
        builder.Property(p => p.StripePriceIdAnnual).HasMaxLength(256);
        builder.Property(p => p.StripeProductId).HasMaxLength(256);

        builder.HasQueryFilter(p => !p.IsDeleted);
    }
}
