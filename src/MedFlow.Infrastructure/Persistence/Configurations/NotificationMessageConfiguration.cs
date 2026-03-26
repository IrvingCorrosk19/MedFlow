using MedFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MedFlow.Infrastructure.Persistence.Configurations;

public class NotificationMessageConfiguration : IEntityTypeConfiguration<NotificationMessage>
{
    public void Configure(EntityTypeBuilder<NotificationMessage> builder)
    {
        builder.ToTable("NotificationMessages");

        builder.HasIndex(m => new { m.TenantId, m.CreatedAt });
        builder.HasIndex(m => m.ExternalId).HasFilter("\"ExternalId\" IS NOT NULL");
        builder.HasIndex(m => new { m.EventType, m.Status });

        builder.Property(m => m.Recipient).HasMaxLength(512);
        builder.Property(m => m.Subject).HasMaxLength(500);
        builder.Property(m => m.Body).HasMaxLength(16000);
        builder.Property(m => m.ExternalId).HasMaxLength(256);
        builder.Property(m => m.ErrorMessage).HasMaxLength(2000);
        builder.Property(m => m.RelatedEntityType).HasMaxLength(64);
        builder.Property(m => m.RelatedEntityId).HasMaxLength(64);
        builder.Property(m => m.WebhookPayload).HasMaxLength(16000);
        builder.Property(m => m.WebhookResponse).HasMaxLength(4000);

        builder.HasOne(m => m.Tenant)
            .WithMany()
            .HasForeignKey(m => m.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(m => m.Template)
            .WithMany()
            .HasForeignKey(m => m.TemplateId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
