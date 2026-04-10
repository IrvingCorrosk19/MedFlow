using MedFlow.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MedFlow.Web.Controllers.Api;

/// <summary>
/// Returns vital sign history for a patient to power trend charts.
/// </summary>
[Authorize]
[Route("api/patients/{patientId:guid}/vitals")]
[ApiController]
public class PatientVitalsController : ControllerBase
{
    private readonly IApplicationDbContext _db;
    private readonly ITenantContext _tenant;

    public PatientVitalsController(IApplicationDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    [HttpGet]
    public async Task<IActionResult> GetVitals(Guid patientId, CancellationToken ct)
    {
        if (!_tenant.TenantId.HasValue) return Forbid();
        var tid = _tenant.TenantId.Value;

        var records = await _db.MedicalRecords
            .AsNoTracking()
            .Where(mr => mr.TenantId == tid
                && mr.PatientId == patientId
                && !mr.IsDeleted
                && (mr.WeightKg.HasValue || mr.HeightCm.HasValue
                    || mr.BloodPressure != null || mr.HeartRateBpm.HasValue
                    || mr.TemperatureCelsius.HasValue))
            .OrderBy(mr => mr.VisitDate)
            .Take(20)
            .Select(mr => new
            {
                date = mr.VisitDate.ToString("dd/MM/yy"),
                weight = mr.WeightKg,
                height = mr.HeightCm,
                heartRate = mr.HeartRateBpm,
                temperature = mr.TemperatureCelsius,
                bloodPressure = mr.BloodPressure
            })
            .ToListAsync(ct);

        return Ok(records);
    }
}
