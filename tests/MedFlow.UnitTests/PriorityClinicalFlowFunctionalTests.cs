using MedFlow.Application.Interfaces;
using MedFlow.Application.Saas;
using MedFlow.Domain.Entities;
using MedFlow.Domain.Enums;
using MedFlow.Infrastructure.Services;
using MedFlow.UnitTests.Helpers;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace MedFlow.UnitTests;

/// <summary>
/// Flujos funcionales alineados con <c>PRUEBAS_FLUJOS_PRIORITARIOS_CLINICAS.md</c> (P0 operación diaria, P1 clínica y tesorería).
/// Ejecutan la cadena real de servicios sobre EF InMemory con un tenant aislado.
/// </summary>
public class PriorityClinicalFlowFunctionalTests
{
    private static void AllowPatientAndAppointment(Mock<ISubscriptionLimitService> limits)
    {
        limits.Setup(l => l.CanCreatePatientAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LimitCheckResult(true, null, null));
        limits.Setup(l => l.CanCreateAppointmentAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LimitCheckResult(true, null, null));
    }

    /// <summary>P0: Identidad de negocio — registrar paciente, agendar cita y cerrar visita (estado completada).</summary>
    [Fact]
    public async Task P0_ReceptionYAgenda_RegistraPaciente_CreaCita_Y_CompletaVisita()
    {
        var tenantId = Guid.NewGuid();
        var db = DbContextFactory.Create(tenantId);
        var tenant = new Mock<ITenantContext>();
        tenant.Setup(t => t.TenantId).Returns(tenantId);

        var limits = new Mock<ISubscriptionLimitService>();
        AllowPatientAndAppointment(limits);

        var eventLog = new Mock<IEventLogService>();
        eventLog.Setup(e => e.EnqueueAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var audit = new Mock<IAuditLogService>();
        audit.Setup(a => a.LogAsync(It.IsAny<AuditLogWriteDto>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var notifications = new Mock<INotificationDispatchService>();
        notifications.Setup(n => n.DispatchAsync(It.IsAny<MedFlow.Application.Notifications.DispatchRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DispatchResult(true, Array.Empty<Guid>(), Array.Empty<string>()));

        var patientSvc = new PatientService(db, tenant.Object, MockClinicalUserScope.NoDoctorRestriction(), limits.Object, eventLog.Object, audit.Object);
        var aptSvc = new AppointmentService(db, tenant.Object, MockClinicalUserScope.NoDoctorRestriction(), limits.Object, eventLog.Object, audit.Object, notifications.Object);

        var (_, pErr) = await patientSvc.CreateAsync(SeedData.Patient(tenantId, "Prioridad", "Paciente"));
        Assert.Null(pErr);

        var doctor = SeedData.Doctor(tenantId, "Dr", "Flujo");
        db.Doctors.Add(doctor);
        await db.SaveChangesAsync();

        var patientId = (await db.Patients.FirstAsync()).Id;
        var apt = SeedData.Appointment(tenantId, doctor.Id, patientId);
        var (ok, err) = await aptSvc.CreateAsync(apt);
        Assert.True(ok, err);
        Assert.NotEqual(Guid.Empty, apt.Id);

        apt.Status = AppointmentStatus.Completed;
        var (updOk, updErr) = await aptSvc.UpdateAsync(apt);
        Assert.True(updOk, updErr);

        var persisted = await db.Appointments.AsNoTracking().FirstAsync(a => a.Id == apt.Id);
        Assert.Equal(AppointmentStatus.Completed, persisted.Status);
    }

    /// <summary>P1: Continuidad clínica — historia + receta emitida.</summary>
    [Fact]
    public async Task P1_Doctor_HistoriaClinica_Y_Receta_VinculadasAlPaciente()
    {
        var tenantId = Guid.NewGuid();
        var db = DbContextFactory.Create(tenantId);
        var rxSvc = new PrescriptionService(db);

        var patient = SeedData.Patient(tenantId, "Receta", "Test");
        var doctor = SeedData.Doctor(tenantId);
        db.Patients.Add(patient);
        db.Doctors.Add(doctor);
        await db.SaveChangesAsync();

        var mr = new MedicalRecord
        {
            TenantId = tenantId,
            PatientId = patient.Id,
            DoctorId = doctor.Id,
            VisitDate = DateTime.UtcNow.Date
        };
        db.MedicalRecords.Add(mr);
        await db.SaveChangesAsync();

        var prescription = new Prescription
        {
            TenantId = tenantId,
            MedicalRecordId = mr.Id,
            MedicationName = "Paracetamol",
            Dosage = "500 mg",
            IssuedAt = DateTime.UtcNow
        };
        var created = await rxSvc.CreateAsync(prescription);

        var byPatient = await rxSvc.GetByPatientAsync(patient.Id, tenantId, CancellationToken.None);
        Assert.Single(byPatient);
        Assert.Equal(created.Id, byPatient[0].Id);
    }

    /// <summary>P1: Facturación — factura con líneas y totales coherentes.</summary>
    [Fact]
    public async Task P1_Billing_GeneraFacturaValidaConTotales()
    {
        var tenantId = Guid.NewGuid();
        var db = DbContextFactory.Create(tenantId);
        var tenantCtx = new Mock<ITenantContext>();
        tenantCtx.Setup(t => t.TenantId).Returns(tenantId);
        tenantCtx.Setup(t => t.IgnoreTenantFilter).Returns(false);
        var planFeatures = new Mock<IPlanFeatureService>();
        planFeatures.Setup(p => p.HasBillingModuleAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var svc = new BillingInvoiceService(
            db,
            tenantCtx.Object,
            planFeatures.Object,
            new Mock<IEventLogService>().Object,
            new Mock<IAuditLogService>().Object,
            journalEntries: null);

        db.Patients.Add(SeedData.Patient(tenantId));
        await db.SaveChangesAsync();
        var patientId = (await db.Patients.FirstAsync()).Id;

        var invoice = new BillingInvoice
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            PatientId = patientId,
            IssueDate = DateTime.UtcNow,
            DiscountAmount = 0m,
            TaxAmount = 0m,
            Status = InvoiceStatus.Pending
        };
        var items = new[]
        {
            new BillingInvoiceItem
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                ItemType = BillingInvoiceItemType.ConsultationGeneral,
                Description = "Consulta",
                Quantity = 1,
                UnitPrice = 100m,
                DiscountAmount = 0m
            }
        };

        var (result, error) = await svc.CreateAsync(invoice, items);

        Assert.Null(error);
        Assert.NotNull(result);
        Assert.Equal(100m, result.TotalAmount);
        Assert.Equal(100m, result.BalanceDue);
    }

    /// <summary>P1: Tesorería — movimiento de caja registrado y recuperable por rango.</summary>
    [Fact]
    public async Task P1_Tesoreria_RegistraMovimientoCaja_ConsultablePorFecha()
    {
        var tenantId = Guid.NewGuid();
        var db = DbContextFactory.Create(tenantId);
        var svc = new CashMovementService(db);

        var day = DateTime.UtcNow.Date;
        var (created, err) = await svc.CreateAsync(new CashMovement
        {
            TenantId = tenantId,
            MovementType = CashMovementType.Income,
            Amount = 75m,
            Description = "Cobro consulta",
            MovementDate = day,
            CreatedByUserId = "functional-test"
        });

        Assert.Null(err);
        Assert.NotNull(created);

        var list = await svc.GetByDateRangeAsync(day, day.AddDays(1));
        Assert.Single(list);
        Assert.Equal(75m, list[0].Amount);
    }
}
