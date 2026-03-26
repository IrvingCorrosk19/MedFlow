using MedFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MedFlow.Infrastructure.Persistence.Configurations;

public class TenantSettingsConfiguration : IEntityTypeConfiguration<TenantSettings>
{
    public void Configure(EntityTypeBuilder<TenantSettings> builder)
    {
        builder.ToTable("TenantSettings");

        builder.HasIndex(x => new { x.TenantId, x.SettingKey }).IsUnique();

        builder.Property(x => x.SettingKey).HasMaxLength(128).IsRequired();
        builder.Property(x => x.SettingValue).HasMaxLength(8000);

        builder.HasOne(x => x.Tenant)
            .WithMany(t => t.TenantSettings)
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
