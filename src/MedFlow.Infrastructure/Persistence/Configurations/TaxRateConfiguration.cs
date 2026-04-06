using MedFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MedFlow.Infrastructure.Persistence.Configurations;

public class TaxRateConfiguration : IEntityTypeConfiguration<TaxRate>
{
    public void Configure(EntityTypeBuilder<TaxRate> builder)
    {
        builder.ToTable("TaxRates");

        builder.Property(t => t.Name).HasMaxLength(100).IsRequired();
        builder.Property(t => t.Code).HasMaxLength(20).IsRequired();
        builder.Property(t => t.Rate).HasPrecision(8, 4);

        builder.HasIndex(t => new { t.TenantId, t.Code }).IsUnique();

        builder.HasOne(t => t.TaxAccount)
            .WithMany()
            .HasForeignKey(t => t.TaxAccountId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Property(t => t.TenantId).IsRequired();
        builder.HasIndex(t => t.TenantId);
        builder.HasIndex(t => new { t.TenantId, t.IsDefault });

        builder.HasOne(t => t.Tenant)
            .WithMany()
            .HasForeignKey(t => t.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
