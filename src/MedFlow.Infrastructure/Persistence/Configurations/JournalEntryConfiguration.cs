using MedFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MedFlow.Infrastructure.Persistence.Configurations;

public class JournalEntryConfiguration : IEntityTypeConfiguration<JournalEntry>
{
    public void Configure(EntityTypeBuilder<JournalEntry> builder)
    {
        builder.ToTable("JournalEntries");

        builder.Property(j => j.EntryNumber).HasMaxLength(30).IsRequired();
        builder.HasIndex(j => new { j.TenantId, j.EntryNumber }).IsUnique();

        builder.Property(j => j.Description).HasMaxLength(500).IsRequired();
        builder.Property(j => j.Reference).HasMaxLength(200);
        builder.Property(j => j.CreatedByUserId).HasMaxLength(450);
        builder.Property(j => j.TotalDebit).HasPrecision(18, 2);
        builder.Property(j => j.TotalCredit).HasPrecision(18, 2);

        builder.HasOne(j => j.FiscalPeriod)
            .WithMany(fp => fp.JournalEntries)
            .HasForeignKey(j => j.FiscalPeriodId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(j => j.TenantId).IsRequired();
        builder.HasIndex(j => j.TenantId);
        builder.HasIndex(j => new { j.TenantId, j.EntryDate });
        builder.HasIndex(j => new { j.TenantId, j.Status });
        builder.HasIndex(j => new { j.TenantId, j.Origin });
        builder.HasIndex(j => j.SourceDocumentId);

        builder.HasOne(j => j.Tenant)
            .WithMany()
            .HasForeignKey(j => j.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
