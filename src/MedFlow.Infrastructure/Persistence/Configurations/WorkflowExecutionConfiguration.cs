using MedFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MedFlow.Infrastructure.Persistence.Configurations;

public class WorkflowExecutionConfiguration : IEntityTypeConfiguration<WorkflowExecution>
{
    public void Configure(EntityTypeBuilder<WorkflowExecution> builder)
    {
        builder.ToTable("WorkflowExecutions");

        builder.Property(e => e.EventType).HasMaxLength(100).IsRequired();
        builder.Property(e => e.AggregateId).HasMaxLength(64);
        builder.Property(e => e.PayloadJson).IsRequired();
        builder.Property(e => e.ResponseBody).HasMaxLength(8000);
        builder.Property(e => e.ErrorMessage).HasMaxLength(4000);

        builder.HasIndex(e => new { e.TenantId, e.Status, e.CreatedAt });
        builder.HasIndex(e => new { e.WorkflowDefinitionId, e.Status });
        builder.HasIndex(e => new { e.Status, e.NextAttemptAt }).HasFilter("\"NextAttemptAt\" IS NOT NULL");

        builder.HasOne(e => e.Tenant)
            .WithMany()
            .HasForeignKey(e => e.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.WorkflowDefinition)
            .WithMany(w => w.Executions)
            .HasForeignKey(e => e.WorkflowDefinitionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
