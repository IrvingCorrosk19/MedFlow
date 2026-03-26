using MedFlow.Domain.Entities;
using MedFlow.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MedFlow.Infrastructure.Persistence.Configurations;

public class AIInsightConfiguration : IEntityTypeConfiguration<AIInsight>
{
    public void Configure(EntityTypeBuilder<AIInsight> builder)
    {
        builder.ToTable("AIInsights");

        builder.Property(i => i.EntityType).HasMaxLength(64).IsRequired();
        builder.Property(i => i.EntityId).HasMaxLength(64);
        builder.Property(i => i.Title).HasMaxLength(300).IsRequired();
        builder.Property(i => i.Summary).HasMaxLength(2000).IsRequired();
        builder.Property(i => i.Recommendation).HasMaxLength(1000);
        builder.Property(i => i.EvidenceJson).HasMaxLength(8000);
        builder.Property(i => i.Source).HasMaxLength(64).IsRequired();
        builder.Property(i => i.AcknowledgedByUserId).HasMaxLength(450);

        builder.HasIndex(i => new { i.TenantId, i.InsightType, i.GeneratedAt });
        builder.HasIndex(i => new { i.TenantId, i.Status, i.GeneratedAt });
        builder.HasIndex(i => new { i.TenantId, i.EntityType, i.EntityId });
        builder.HasIndex(i => new { i.TenantId, i.Severity });

        builder.HasOne(i => i.Tenant)
            .WithMany()
            .HasForeignKey(i => i.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
