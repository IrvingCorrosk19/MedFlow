using MedFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MedFlow.Infrastructure.Persistence.Configurations;

public class DoctorConfiguration : IEntityTypeConfiguration<Doctor>
{
    public void Configure(EntityTypeBuilder<Doctor> builder)
    {
        builder.ToTable("Doctors");

        builder.Property(d => d.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(d => d.LastName).HasMaxLength(100).IsRequired();
        builder.Property(d => d.Speciality).HasMaxLength(100);
        builder.Property(d => d.LicenseNumber).HasMaxLength(50);
        builder.Property(d => d.Phone).HasMaxLength(30);
        builder.Property(d => d.Email).HasMaxLength(256);

        builder.Property(d => d.TenantId).IsRequired();
        builder.HasIndex(d => d.TenantId);
        builder.HasIndex(d => new { d.TenantId, d.LastName });

        builder.HasOne(d => d.Tenant)
            .WithMany()
            .HasForeignKey(d => d.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(d => d.UserId);
    }
}
