using MedFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MedFlow.Infrastructure.Persistence.Configurations;

public class NotificationPreferenceConfiguration : IEntityTypeConfiguration<NotificationPreference>
{
    public void Configure(EntityTypeBuilder<NotificationPreference> builder)
    {
        builder.ToTable("NotificationPreferences");

        builder.HasIndex(p => new { p.TenantId, p.EventType, p.Channel }).IsUnique();

        builder.Property(p => p.OverrideRecipient).HasMaxLength(512);
        builder.Property(p => p.OverrideWebhookUrl).HasMaxLength(2048);
        builder.Property(p => p.Notes).HasMaxLength(500);

        builder.HasOne(p => p.Tenant)
            .WithMany()
            .HasForeignKey(p => p.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.Template)
            .WithMany()
            .HasForeignKey(p => p.TemplateId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
