using MedFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MedFlow.Infrastructure.Persistence.Configurations;

public class NotificationTemplateConfiguration : IEntityTypeConfiguration<NotificationTemplate>
{
    public void Configure(EntityTypeBuilder<NotificationTemplate> builder)
    {
        builder.ToTable("NotificationTemplates");

        builder.HasIndex(t => new { t.TenantId, t.EventType, t.Channel });
        builder.HasIndex(t => new { t.TenantId, t.Code }).IsUnique();

        builder.Property(t => t.Code).HasMaxLength(64);
        builder.Property(t => t.Name).HasMaxLength(200);
        builder.Property(t => t.SubjectTemplate).HasMaxLength(500);
        builder.Property(t => t.BodyTemplate).HasMaxLength(8000);
        builder.Property(t => t.HtmlBodyTemplate).HasMaxLength(16000);
        builder.Property(t => t.FromEmail).HasMaxLength(256);
        builder.Property(t => t.FromName).HasMaxLength(200);
        builder.Property(t => t.ReplyTo).HasMaxLength(256);
        builder.Property(t => t.WebhookUrl).HasMaxLength(2048);
        builder.Property(t => t.WebhookMethod).HasMaxLength(16);
        builder.Property(t => t.ResendTemplateId).HasMaxLength(128);
        builder.Property(t => t.WhatsAppTemplateId).HasMaxLength(128);
        builder.Property(t => t.Description).HasMaxLength(500);

        builder.HasOne(t => t.Tenant)
            .WithMany()
            .HasForeignKey(t => t.TenantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
