using MedFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MedFlow.Infrastructure.Persistence.Configurations;

public class MedicalAttachmentConfiguration : IEntityTypeConfiguration<MedicalAttachment>
{
    public void Configure(EntityTypeBuilder<MedicalAttachment> builder)
    {
        builder.ToTable("MedicalAttachments");

        builder.Property(a => a.FileName).HasMaxLength(500).IsRequired();
        builder.Property(a => a.FilePath).HasMaxLength(1000).IsRequired();
        builder.Property(a => a.ContentType).HasMaxLength(200);

        builder.HasOne(a => a.MedicalRecord)
            .WithMany(m => m.Attachments)
            .HasForeignKey(a => a.MedicalRecordId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(a => a.TenantId).IsRequired();
        builder.HasIndex(a => a.TenantId);

        builder.HasOne(a => a.Tenant)
            .WithMany()
            .HasForeignKey(a => a.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
