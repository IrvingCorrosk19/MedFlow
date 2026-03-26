using MedFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MedFlow.Infrastructure.Persistence.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");

        builder.Property(a => a.UserName).HasMaxLength(256);
        builder.Property(a => a.Action).HasMaxLength(100).IsRequired();
        builder.Property(a => a.Module).HasMaxLength(100).IsRequired();
        builder.Property(a => a.EntityName).HasMaxLength(200);
        builder.Property(a => a.EntityId).HasMaxLength(64);
        builder.Property(a => a.Description).HasMaxLength(2000);
        builder.Property(a => a.IpAddress).HasMaxLength(64);

        builder.Property(a => a.TenantId).IsRequired();
        builder.HasIndex(a => a.TenantId);
        builder.HasIndex(a => new { a.TenantId, a.CreatedAt });
        builder.HasIndex(a => a.CreatedAt);
        builder.HasIndex(a => a.Module);
        builder.HasIndex(a => a.UserId);

        builder.HasOne(a => a.Tenant)
            .WithMany()
            .HasForeignKey(a => a.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
