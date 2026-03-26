using MedFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MedFlow.Infrastructure.Persistence.Configurations;

public class WorkerHeartbeatConfiguration : IEntityTypeConfiguration<WorkerHeartbeat>
{
    public void Configure(EntityTypeBuilder<WorkerHeartbeat> builder)
    {
        builder.ToTable("WorkerHeartbeats");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.WorkerName).IsUnique();
        builder.Property(x => x.WorkerName).HasMaxLength(64);
        builder.Property(x => x.Status).HasMaxLength(32);
    }
}
