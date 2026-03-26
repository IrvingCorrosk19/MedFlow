using MedFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MedFlow.Infrastructure.Persistence.Configurations;

public class StripeWebhookEventLogConfiguration : IEntityTypeConfiguration<StripeWebhookEventLog>
{
    public void Configure(EntityTypeBuilder<StripeWebhookEventLog> builder)
    {
        builder.ToTable("StripeWebhookEventLogs");

        builder.HasIndex(e => e.ProviderEventId).IsUnique();
        builder.HasIndex(e => e.EventType);
        builder.HasIndex(e => e.ProcessedAt);

        builder.Property(e => e.ProviderEventId).HasMaxLength(256);
        builder.Property(e => e.EventType).HasMaxLength(128);
    }
}
