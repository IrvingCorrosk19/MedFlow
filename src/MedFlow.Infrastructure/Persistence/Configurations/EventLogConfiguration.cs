using MedFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MedFlow.Infrastructure.Persistence.Configurations;

public class EventLogConfiguration : IEntityTypeConfiguration<EventLog>
{
    public void Configure(EntityTypeBuilder<EventLog> builder)
    {
        builder.ToTable("EventLogs");

        builder.Property(e => e.EventType).HasMaxLength(100).IsRequired();
        builder.Property(e => e.AggregateType).HasMaxLength(100);
        builder.Property(e => e.AggregateId).HasMaxLength(64);
        builder.Property(e => e.PayloadJson).IsRequired();
        builder.Property(e => e.LastError).HasMaxLength(4000);

        builder.Property(e => e.TenantId).IsRequired();
        builder.HasIndex(e => e.TenantId);
        builder.HasIndex(e => new { e.TenantId, e.Status, e.CreatedAt });

        builder.HasOne(e => e.Tenant)
            .WithMany()
            .HasForeignKey(e => e.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => new { e.Status, e.CreatedAt });
        builder.HasIndex(e => e.EventType);
    }
}
