using MedFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MedFlow.Infrastructure.Persistence.Configurations;

public class SaaSBillingTransactionConfiguration : IEntityTypeConfiguration<SaaSBillingTransaction>
{
    public void Configure(EntityTypeBuilder<SaaSBillingTransaction> builder)
    {
        builder.ToTable("SaaSBillingTransactions");

        builder.HasIndex(t => t.TenantId);
        builder.HasIndex(t => t.TenantSubscriptionId);
        builder.HasIndex(t => t.OccurredAt);
        builder.HasIndex(t => t.ProviderTransactionId).HasFilter("\"ProviderTransactionId\" IS NOT NULL");

        builder.Property(t => t.ProviderTransactionId).HasMaxLength(256);
        builder.Property(t => t.ProviderInvoiceId).HasMaxLength(256);
        builder.Property(t => t.ProviderPaymentIntentId).HasMaxLength(256);
        builder.Property(t => t.Currency).HasMaxLength(8);
        builder.Property(t => t.Description).HasMaxLength(500);
        builder.Property(t => t.FailureReason).HasMaxLength(1000);

        builder.HasOne(t => t.Tenant)
            .WithMany()
            .HasForeignKey(t => t.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.TenantSubscription)
            .WithMany()
            .HasForeignKey(t => t.TenantSubscriptionId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
