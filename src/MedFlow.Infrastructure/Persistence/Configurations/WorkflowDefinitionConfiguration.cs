using MedFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MedFlow.Infrastructure.Persistence.Configurations;

public class WorkflowDefinitionConfiguration : IEntityTypeConfiguration<WorkflowDefinition>
{
    public void Configure(EntityTypeBuilder<WorkflowDefinition> builder)
    {
        builder.ToTable("WorkflowDefinitions");

        builder.Property(w => w.Name).HasMaxLength(200).IsRequired();
        builder.Property(w => w.Code).HasMaxLength(64).IsRequired();
        builder.Property(w => w.Description).HasMaxLength(500);
        builder.Property(w => w.TriggerEvent).HasMaxLength(100).IsRequired();
        builder.Property(w => w.WebhookUrl).HasMaxLength(2048).IsRequired();
        builder.Property(w => w.HttpMethod).HasMaxLength(16).IsRequired();
        builder.Property(w => w.HeadersJson).HasMaxLength(4000);
        builder.Property(w => w.PayloadTemplateJson).IsRequired();
        builder.Property(w => w.RetryPolicyJson).HasMaxLength(2000);
        builder.Property(w => w.TimeoutSeconds);

        builder.HasIndex(w => new { w.TenantId, w.TriggerEvent });
        builder.HasIndex(w => new { w.TenantId, w.Code }).IsUnique();
        builder.HasIndex(w => new { w.TenantId, w.IsActive, w.TriggerEvent });

        builder.HasOne(w => w.Tenant)
            .WithMany()
            .HasForeignKey(w => w.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
