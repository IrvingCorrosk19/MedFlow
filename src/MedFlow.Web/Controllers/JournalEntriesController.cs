using MedFlow.Application.Accounting;
using MedFlow.Application.Interfaces;
using MedFlow.Application.Security;
using MedFlow.Domain.Enums;
using MedFlow.Web.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;

namespace MedFlow.Web.Controllers;

[Authorize]
public class JournalEntriesController : Controller
{
    private readonly IJournalEntryService _journalEntries;
    private readonly IAccountService _accounts;
    private readonly IFiscalPeriodService _fiscalPeriods;
    private readonly ITenantContext _tenant;

    public JournalEntriesController(
        IJournalEntryService journalEntries,
        IAccountService accounts,
        IFiscalPeriodService fiscalPeriods,
        ITenantContext tenant)
    {
        _journalEntries = journalEntries;
        _accounts = accounts;
        _fiscalPeriods = fiscalPeriods;
        _tenant = tenant;
    }

    [RequirePermission(PermissionCodes.AccountingView)]
    public async Task<IActionResult> Index(
        DateTime? from, DateTime? to,
        JournalEntryStatus? status, JournalEntryOrigin? origin,
        string? reference, CancellationToken ct)
    {
        if (!_tenant.TenantId.HasValue) return NotFound();
        var list = await _journalEntries.SearchAsync(_tenant.TenantId.Value, from, to, status, origin, reference, ct);
        ViewBag.From = from?.ToString("yyyy-MM-dd");
        ViewBag.To = to?.ToString("yyyy-MM-dd");
        ViewBag.Status = status;
        ViewBag.Origin = origin;
        ViewBag.Reference = reference;
        ViewBag.TotalCount = list.Count;
        ViewData["Title"] = "Asientos Contables";
        ViewData["PageSubtitle"] = "Partida doble";
        ViewData["Breadcrumb"] = "<li class=\"breadcrumb-item active\">Asientos</li>";
        var limited = list.Count > 100 ? list.Take(100).ToList() : list.ToList();
        return View(limited);
    }

    [RequirePermission(PermissionCodes.AccountingView)]
    public async Task<IActionResult> Details(Guid id, CancellationToken ct)
    {
        if (!_tenant.TenantId.HasValue) return NotFound();
        var detail = await _journalEntries.GetDetailAsync(id, _tenant.TenantId.Value, ct);
        if (detail is null) return NotFound();
        ViewData["Title"] = $"Asiento {detail.EntryNumber}";
        return View(detail);
    }

    [HttpGet]
    [RequirePermission(PermissionCodes.AccountingManage)]
    public async Task<IActionResult> Create(CancellationToken ct)
    {
        if (!_tenant.TenantId.HasValue) return NotFound();
        await PopulateViewBagAsync(ct);
        var vm = new JournalEntryFormDto(
            null, DateTime.Today, Guid.Empty, "", null,
            new List<JournalEntryLineFormDto>
            {
                new(null, Guid.Empty, null, 0, 0),
                new(null, Guid.Empty, null, 0, 0)
            });
        ViewData["Title"] = "Nuevo Asiento Contable";
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    [RequirePermission(PermissionCodes.AccountingManage)]
    public async Task<IActionResult> Create(JournalEntryFormDto form, CancellationToken ct)
    {
        if (!_tenant.TenantId.HasValue) return NotFound();
        if (!ModelState.IsValid)
        {
            await PopulateViewBagAsync(ct);
            return View(form);
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
        var (entry, error) = await _journalEntries.CreateAsync(_tenant.TenantId.Value, form, userId, ct);
        if (error is not null)
        {
            ModelState.AddModelError(string.Empty, error);
            await PopulateViewBagAsync(ct);
            return View(form);
        }

        TempData["Success"] = $"Asiento {entry.EntryNumber} creado.";
        return RedirectToAction(nameof(Details), new { id = entry.Id });
    }

    [HttpGet]
    [RequirePermission(PermissionCodes.AccountingManage)]
    public async Task<IActionResult> Edit(Guid id, CancellationToken ct)
    {
        if (!_tenant.TenantId.HasValue) return NotFound();
        var detail = await _journalEntries.GetDetailAsync(id, _tenant.TenantId.Value, ct);
        if (detail is null) return NotFound();
        if (detail.Status != JournalEntryStatus.Draft)
        {
            TempData["Error"] = "Solo se pueden editar asientos en estado Borrador.";
            return RedirectToAction(nameof(Details), new { id });
        }

        await PopulateViewBagAsync(ct);
        var form = new JournalEntryFormDto(
            detail.Id,
            detail.EntryDate,
            detail.FiscalPeriodId,
            detail.Description,
            detail.Reference,
            detail.Lines.Select(l => new JournalEntryLineFormDto(l.Id, l.AccountId, l.Description, l.Debit, l.Credit)).ToList()
        );
        ViewData["Title"] = $"Editar Asiento {detail.EntryNumber}";
        return View(form);
    }

    [HttpPost, ValidateAntiForgeryToken]
    [RequirePermission(PermissionCodes.AccountingManage)]
    public async Task<IActionResult> Edit(Guid id, JournalEntryFormDto form, CancellationToken ct)
    {
        if (!_tenant.TenantId.HasValue) return NotFound();
        if (!ModelState.IsValid)
        {
            await PopulateViewBagAsync(ct);
            return View(form);
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var (ok, error) = await _journalEntries.UpdateAsync(id, form, userId, ct);
        if (!ok)
        {
            ModelState.AddModelError(string.Empty, error ?? "Error al actualizar el asiento.");
            await PopulateViewBagAsync(ct);
            return View(form);
        }

        TempData["Success"] = "Asiento actualizado correctamente.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [RequirePermission(PermissionCodes.AccountingPost)]
    public async Task<IActionResult> Post(Guid id, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
        var (ok, error) = await _journalEntries.PostAsync(id, userId, ct);
        if (!ok) TempData["Error"] = error;
        else TempData["Success"] = "Asiento contabilizado correctamente.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [RequirePermission(PermissionCodes.AccountingManage)]
    public async Task<IActionResult> Void(Guid id, string reason, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
        var (ok, error) = await _journalEntries.VoidAsync(id, userId, reason ?? "Sin motivo", ct);
        if (!ok) TempData["Error"] = error;
        else TempData["Success"] = "Asiento anulado.";
        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task PopulateViewBagAsync(CancellationToken ct)
    {
        if (!_tenant.TenantId.HasValue) return;
        var tenantId = _tenant.TenantId.Value;

        // Ensure the current month's fiscal period exists
        var now = DateTime.UtcNow;
        await _fiscalPeriods.GetOrCreateAsync(tenantId, now.Year, now.Month, ct);

        var accounts = await _accounts.GetAllAsync(tenantId, ct);
        var periods = await _fiscalPeriods.GetAllAsync(tenantId, ct);

        ViewBag.Accounts = new SelectList(
            accounts.Where(a => a.AllowsDirectPosting).Select(a => new { a.Id, Name = $"{a.Code} - {a.Name}" }),
            "Id", "Name");
        ViewBag.FiscalPeriods = new SelectList(
            periods.Where(p => p.Status == MedFlow.Domain.Enums.FiscalPeriodStatus.Open)
                   .Select(p => new { p.Id, p.Name }),
            "Id", "Name");
    }
}
