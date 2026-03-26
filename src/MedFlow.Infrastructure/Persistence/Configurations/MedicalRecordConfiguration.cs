using MedFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MedFlow.Infrastructure.Persistence.Configurations;

public class MedicalRecordConfiguration : IEntityTypeConfiguration<MedicalRecord>
{
    public void Configure(EntityTypeBuilder<MedicalRecord> builder)
    {
        builder.ToTable("MedicalRecords");

        builder.Property(m => m.ChiefComplaint).HasMaxLength(500);
        builder.Property(m => m.Diagnosis).HasMaxLength(500);
        builder.Property(m => m.TreatmentPlan).HasMaxLength(4000);
        builder.Property(m => m.ClinicalNotes).HasMaxLength(8000);
        builder.Property(m => m.Observations).HasMaxLength(4000);
        builder.Property(m => m.BloodPressure).HasMaxLength(20);
        builder.Property(m => m.HeightCm).HasPrecision(6, 2);
        builder.Property(m => m.WeightKg).HasPrecision(6, 2);
        builder.Property(m => m.TemperatureCelsius).HasPrecision(4, 1);

        builder.HasOne(m => m.Patient)
            .WithMany(p => p.MedicalRecords)
            .HasForeignKey(m => m.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.Doctor)
            .WithMany(d => d.MedicalRecords)
            .HasForeignKey(m => m.DoctorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.Appointment)
            .WithMany()
            .HasForeignKey(m => m.AppointmentId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Property(m => m.TenantId).IsRequired();
        builder.HasIndex(m => m.TenantId);
        builder.HasIndex(m => new { m.TenantId, m.VisitDate });

        builder.HasOne(m => m.Tenant)
            .WithMany()
            .HasForeignKey(m => m.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(m => new { m.PatientId, m.VisitDate });
    }
}
