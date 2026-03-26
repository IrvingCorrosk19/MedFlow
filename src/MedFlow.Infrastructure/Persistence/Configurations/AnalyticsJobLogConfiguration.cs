using MedFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MedFlow.Infrastructure.Persistence.Configurations;

public class AnalyticsJobLogConfiguration : IEntityTypeConfiguration<AnalyticsJobLog>
{
    public void Configure(EntityTypeBuilder<AnalyticsJobLog> builder)
    {
        builder.ToTable("AnalyticsJobLogs");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.JobType, x.CreatedAt });
        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => x.SnapshotDate);
    }
}
