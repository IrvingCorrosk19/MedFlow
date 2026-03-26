using MedFlow.Application.Interfaces;
using MedFlow.Application.PatientPortal;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedFlow.Web.Controllers.Api.Mobile.V1;

[ApiController]
[Route("api/v1/mobile/payments")]
[Authorize(AuthenticationSchemes = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme)]
public class MobilePaymentsController : ControllerBase
{
    private readonly IPatientPortalService _portal;

    public MobilePaymentsController(IPatientPortalService portal)
    {
        _portal = portal;
    }

    private async Task<Guid?> GetPatientIdAsync(CancellationToken ct)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return null;
        return await _portal.GetPatientIdByUserIdAsync(userId, ct);
    }

    [HttpGet("invoices")]
    [ProducesResponseType(typeof(IReadOnlyList<PatientInvoiceListItemDto>), 200)]
    public async Task<IActionResult> GetInvoices(CancellationToken ct)
    {
        var patientId = await GetPatientIdAsync(ct);
        if (!patientId.HasValue)
            return Unauthorized();

        var list = await _portal.GetInvoicesAsync(patientId.Value, ct);
        return Ok(list);
    }

    [HttpGet("invoices/{id:guid}")]
    [ProducesResponseType(typeof(PatientInvoiceListItemDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetInvoice(Guid id, CancellationToken ct)
    {
        var patientId = await GetPatientIdAsync(ct);
        if (!patientId.HasValue)
            return Unauthorized();

        var inv = await _portal.GetInvoiceAsync(patientId.Value, id, ct);
        if (inv == null)
            return NotFound();

        return Ok(inv);
    }

    [HttpGet("history")]
    [ProducesResponseType(typeof(IReadOnlyList<PatientPaymentListItemDto>), 200)]
    public async Task<IActionResult> GetPaymentHistory([FromQuery] int take = 50, CancellationToken ct = default)
    {
        var patientId = await GetPatientIdAsync(ct);
        if (!patientId.HasValue)
            return Unauthorized();

        var list = await _portal.GetPaymentsAsync(patientId.Value, take, ct);
        return Ok(list);
    }

    [HttpGet("account-status")]
    [ProducesResponseType(typeof(AccountStatusResponse), 200)]
    public async Task<IActionResult> GetAccountStatus(CancellationToken ct)
    {
        var patientId = await GetPatientIdAsync(ct);
        if (!patientId.HasValue)
            return Unauthorized();

        var (balanceDue, totalPaid) = await _portal.GetAccountStatusAsync(patientId.Value, ct);
        return Ok(new AccountStatusResponse(balanceDue, totalPaid));
    }
}

public record AccountStatusResponse(decimal BalanceDue, decimal TotalPaid);
