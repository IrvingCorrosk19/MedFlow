using MedFlow.Domain.Entities;
using MedFlow.Infrastructure.Services;
using MedFlow.UnitTests.Helpers;

namespace MedFlow.UnitTests;

public class PrescriptionServiceTests
{
    private static readonly Guid TenantId = Guid.NewGuid();

    private static (PrescriptionService svc, MedFlow.Infrastructure.Persistence.ApplicationDbContext db) Create()
    {
        var db = DbContextFactory.Create(TenantId);
        return (new PrescriptionService(db), db);
    }

    private static async Task<MedicalRecord> SeedMedicalRecordAsync(
        MedFlow.Infrastructure.Persistence.ApplicationDbContext db)
    {
        var patient = new Patient
        {
            TenantId = TenantId,
            PrimerNombre = "Ana",
            PrimerApellido = "García"
        };
        var doctor = new Doctor
        {
            TenantId = TenantId,
            FirstName = "Dr.",
            LastName = "Pérez"
        };
        var mr = new MedicalRecord
        {
            TenantId = TenantId,
            Patient = patient,
            Doctor = doctor,
            VisitDate = DateTime.UtcNow
        };
        db.MedicalRecords.Add(mr);
        await db.SaveChangesAsync();
        return mr;
    }

    [Fact]
    public async Task CreateAsync_StoresPrescription()
    {
        var (svc, db) = Create();
        var mr = await SeedMedicalRecordAsync(db);

        var rx = new Prescription
        {
            TenantId = TenantId,
            MedicalRecordId = mr.Id,
            MedicationName = "Amoxicilina",
            Dosage = "500mg",
            Frequency = "Cada 8 horas",
            Duration = "7 días",
            IssuedAt = DateTime.UtcNow
        };

        var created = await svc.CreateAsync(rx);

        Assert.NotEqual(Guid.Empty, created.Id);
        Assert.Equal("Amoxicilina", created.MedicationName);
    }

    [Fact]
    public async Task GetByMedicalRecordAsync_ReturnsOnlyRecordPrescriptions()
    {
        var (svc, db) = Create();
        var mr = await SeedMedicalRecordAsync(db);
        var mr2 = await SeedMedicalRecordAsync(db);

        await svc.CreateAsync(new Prescription { TenantId = TenantId, MedicalRecordId = mr.Id, MedicationName = "Med A", IssuedAt = DateTime.UtcNow });
        await svc.CreateAsync(new Prescription { TenantId = TenantId, MedicalRecordId = mr.Id, MedicationName = "Med B", IssuedAt = DateTime.UtcNow });
        await svc.CreateAsync(new Prescription { TenantId = TenantId, MedicalRecordId = mr2.Id, MedicationName = "Med C", IssuedAt = DateTime.UtcNow });

        var result = await svc.GetByMedicalRecordAsync(mr.Id);

        Assert.Equal(2, result.Count);
        Assert.All(result, r => Assert.Equal(mr.Id, r.MedicalRecordId));
    }

    [Fact]
    public async Task VoidAsync_MarksAsVoid()
    {
        var (svc, db) = Create();
        var mr = await SeedMedicalRecordAsync(db);

        var rx = await svc.CreateAsync(new Prescription
        {
            TenantId = TenantId,
            MedicalRecordId = mr.Id,
            MedicationName = "Ibuprofen",
            IssuedAt = DateTime.UtcNow
        });

        await svc.VoidAsync(rx.Id, TenantId, "Reacción adversa");

        var voided = await db.Prescriptions.FindAsync(rx.Id);
        Assert.NotNull(voided);
        Assert.True(voided!.IsVoid);
        Assert.Equal("Reacción adversa", voided.VoidReason);
    }

    [Fact]
    public async Task GetRecentAsync_ReturnsLatestFirst()
    {
        var (svc, db) = Create();
        var mr = await SeedMedicalRecordAsync(db);

        var older = await svc.CreateAsync(new Prescription
        {
            TenantId = TenantId,
            MedicalRecordId = mr.Id,
            MedicationName = "Old",
            IssuedAt = DateTime.UtcNow.AddDays(-10)
        });
        var newer = await svc.CreateAsync(new Prescription
        {
            TenantId = TenantId,
            MedicalRecordId = mr.Id,
            MedicationName = "New",
            IssuedAt = DateTime.UtcNow
        });

        var result = await svc.GetRecentAsync(TenantId, 10);

        Assert.Equal(2, result.Count);
        Assert.Equal("New", result[0].MedicationName);
    }

    [Fact]
    public async Task IncrementPrintCountAsync_IncrementsCounter()
    {
        var (svc, db) = Create();
        var mr = await SeedMedicalRecordAsync(db);

        var rx = await svc.CreateAsync(new Prescription
        {
            TenantId = TenantId,
            MedicalRecordId = mr.Id,
            MedicationName = "Paracetamol",
            IssuedAt = DateTime.UtcNow
        });

        Assert.Equal(0, rx.PrintCount);

        await svc.IncrementPrintCountAsync(rx.Id);
        await svc.IncrementPrintCountAsync(rx.Id);

        var updated = await db.Prescriptions.FindAsync(rx.Id);
        Assert.Equal(2, updated!.PrintCount);
    }

    [Fact]
    public async Task UpdateAsync_ModifiesPrescription()
    {
        var (svc, db) = Create();
        var mr = await SeedMedicalRecordAsync(db);

        var rx = await svc.CreateAsync(new Prescription
        {
            TenantId = TenantId,
            MedicalRecordId = mr.Id,
            MedicationName = "Aspirina",
            Dosage = "100mg",
            IssuedAt = DateTime.UtcNow
        });

        rx.MedicationName = "Aspirina 200mg";
        rx.Dosage = "200mg";
        await svc.UpdateAsync(rx);

        var updated = await db.Prescriptions.FindAsync(rx.Id);
        Assert.Equal("Aspirina 200mg", updated!.MedicationName);
        Assert.Equal("200mg", updated.Dosage);
    }
}
