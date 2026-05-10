using MedFlow.Application.Interfaces;
using MedFlow.Domain.Entities;
using MedFlow.Domain.Enums;
using MedFlow.Infrastructure.Tenancy;
using MedFlow.Web.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MedFlow.Web.Controllers.Api;

/// <summary>
/// Global search across patients, appointments, and invoices.
/// Powers the navbar search box.
/// </summary>
[Authorize(Roles = MedFlowStaffRoles.List)]
[Route("api/search")]
[ApiController]
public class GlobalSearchController : ControllerBase
{
    private readonly IApplicationDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly IClinicalUserScope _clinicalScope;

    public GlobalSearchController(IApplicationDbContext db, ITenantContext tenant, IClinicalUserScope clinicalScope)
    {
        _db = db;
        _tenant = tenant;
        _clinicalScope = clinicalScope;
    }

    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] string q, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
            return Ok(new { results = Array.Empty<object>() });

        if (!_tenant.TenantId.HasValue) return Ok(new { results = Array.Empty<object>() });
        var tid = _tenant.TenantId.Value;
        var term = q.Trim().ToLower();

        var (doctorScoped, linkedDoctorId) = await _clinicalScope.GetDoctorDataScopeAsync(ct);

        var results = new List<object>();

        // Search patients (name, document)
        IQueryable<Patient> patientQuery = _db.Patients
            .AsNoTracking()
            .Where(p => p.TenantId == tid && !p.IsDeleted
                && (p.PrimerNombre.ToLower().Contains(term)
                    || p.PrimerApellido.ToLower().Contains(term)
                    || (p.NumeroDocumento != null && p.NumeroDocumento.Contains(term))
                    || (p.SegundoNombre != null && p.SegundoNombre.ToLower().Contains(term))
                    || (p.SegundoApellido != null && p.SegundoApellido.ToLower().Contains(term))));

        if (doctorScoped)
        {
            if (!linkedDoctorId.HasValue)
                patientQuery = patientQuery.Where(p => false);
            else
                patientQuery = ClinicalDoctorPatientFilter.Apply(_db, linkedDoctorId.Value, patientQuery);
        }

        var patients = await patientQuery
            .OrderBy(p => p.PrimerApellido)
            .Take(5)
            .Select(p => new
            {
                type = "patient",
                icon = "fa fa-user text-primary",
                title = p.PrimerNombre + " " + p.PrimerApellido,
                subtitle = p.NumeroDocumento ?? "Sin documento",
                link = "/Patients/Details/" + p.Id
            })
            .ToListAsync(ct);

        results.AddRange(patients);

        // Search invoices (invoice number) — usuarios solo-médico no ven facturas aquí
        if (!doctorScoped && term.Length >= 3)
        {
            var invoices = await _db.BillingInvoices
                .AsNoTracking()
                .Include(i => i.Patient)
                .Where(i => i.TenantId == tid && !i.IsDeleted
                    && i.InvoiceNumber.ToLower().Contains(term))
                .OrderByDescending(i => i.IssueDate)
                .Take(3)
                .Select(i => new
                {
                    type = "invoice",
                    icon = "fa fa-file-text-o text-warning",
                    title = i.InvoiceNumber,
                    subtitle = i.Patient != null ? i.Patient.PrimerNombre + " " + i.Patient.PrimerApellido : "—",
                    link = "/BillingInvoices/Details/" + i.Id
                })
                .ToListAsync(ct);

            results.AddRange(invoices);
        }

        // Search upcoming appointments by patient name
        var now = DateTime.UtcNow;
        var aptQuery = _db.Appointments
            .AsNoTracking()
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .Where(a => a.TenantId == tid && !a.IsDeleted
                && a.Status == AppointmentStatus.Scheduled
                && a.ScheduledDate >= now
                && a.Patient != null
                && (a.Patient.PrimerNombre.ToLower().Contains(term)
                    || a.Patient.PrimerApellido.ToLower().Contains(term)));

        if (doctorScoped)
        {
            if (!linkedDoctorId.HasValue)
                aptQuery = aptQuery.Where(a => false);
            else
                aptQuery = aptQuery.Where(a => a.DoctorId == linkedDoctorId.Value);
        }

        var appointments = await aptQuery
            .OrderBy(a => a.ScheduledDate)
            .Take(3)
            .Select(a => new
            {
                type = "appointment",
                icon = "fa fa-calendar text-success",
                title = "Cita " + a.ScheduledDate.ToString("dd/MM/yyyy HH:mm"),
                subtitle = (a.Patient != null ? a.Patient.PrimerNombre + " " + a.Patient.PrimerApellido : "—")
                         + (a.Doctor != null ? " — Dr. " + a.Doctor.FirstName + " " + a.Doctor.LastName : ""),
                link = "/Appointments/Details/" + a.Id
            })
            .ToListAsync(ct);

        results.AddRange(appointments);

        return Ok(new { results, total = results.Count });
    }
}
