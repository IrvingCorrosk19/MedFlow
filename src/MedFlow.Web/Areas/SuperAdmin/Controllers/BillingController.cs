using MedFlow.Application.Interfaces;
using MedFlow.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedFlow.Web.Areas.SuperAdmin.Controllers;

[Area("SuperAdmin")]
[Authorize(Roles = "SuperAdmin")]
public class BillingController : Controller
{
    private readonly ISaasBillingQueryService _queries;
    private readonly ISaasTenantAdminService _tenants;

    public BillingController(ISaasBillingQueryService queries, ISaasTenantAdminService tenants)
    {
        _queries = queries;
        _tenants = tenants;
    }

    public async Task<IActionResult> Overview(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Billing - Resumen";
        ViewData["PageSubtitle"] = "Panel ejecutivo de facturación SaaS";
        ViewData["Breadcrumb"] = "<li class=\"breadcrumb-item active\">Billing</li>";
        return View(await _queries.GetOverviewAsync(cancellationToken));
    }

    public async Task<IActionResult> Tenants(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Tenants comerciales";
        ViewData["PageSubtitle"] = "Estado de suscripción por clínica";
        ViewData["Breadcrumb"] = "<li class=\"breadcrumb-item\"><a href=\"/SuperAdmin/Billing/Overview\">Billing</a></li><li class=\"breadcrumb-item active\">Tenants</li>";
        return View(await _tenants.GetTenantsAsync(cancellationToken));
    }

    public async Task<IActionResult> Transactions(int skip = 0, int take = 50, Guid? tenantId = null, DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default)
    {
        ViewData["Title"] = "Transacciones SaaS";
        ViewData["PageSubtitle"] = "Historial de cobros y pagos";
        ViewData["Breadcrumb"] = "<li class=\"breadcrumb-item\"><a href=\"/SuperAdmin/Billing/Overview\">Billing</a></li><li class=\"breadcrumb-item active\">Transacciones</li>";
        var list = await _queries.GetTransactionsAsync(skip, take, tenantId, from, to, cancellationToken);
        return View(list);
    }

    public async Task<IActionResult> Invoices(int skip = 0, int take = 50, Guid? tenantId = null, SaaSInvoiceStatus? status = null, CancellationToken cancellationToken = default)
    {
        ViewData["Title"] = "Facturas SaaS";
        ViewData["PageSubtitle"] = "Historial de facturación";
        ViewData["Breadcrumb"] = "<li class=\"breadcrumb-item\"><a href=\"/SuperAdmin/Billing/Overview\">Billing</a></li><li class=\"breadcrumb-item active\">Facturas</li>";
        var list = await _queries.GetInvoicesAsync(skip, take, tenantId, status, cancellationToken);
        return View(list);
    }
}
