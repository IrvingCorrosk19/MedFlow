# MedFlow — Mejoras UI/UX implementadas y backlog

## Implementado

1. **Mission Control** más denso en storytelling financiero/operativo sin saturar: nueva fila `_MissionControlCompareStrip`.
2. **Jerarquía visual**: clase `mf-premium-dashboard-shell` + animación suave (respeta `prefers-reduced-motion`).
3. **Modo oscuro**: gradiente en área de contenido para sensación “app” vs gris plano.
4. **Copilot**: jerarquía tipográfica Plus Jakarta (heredada), panel glass, input grande tipo “prompt”.
5. **Portal legacy**: banner con enlace explícito a `/portal/dashboard`.

## Backlog priorizado (UX)

| Prioridad | Mejora |
|-----------|--------|
| P0 | Estados vacíos globales en listados críticos (pacientes/citas) unificados con `mf-xp-empty` |
| P1 | Skeleton global en dashboard durante carga (parcialmente CSS existe; falta activar en Razor hasta primer paint) |
| P1 | Tablas: extender `mf-xp-table-wrap` a más vistas |
| P2 | Command palette: más destinos (Reports, Settings) |
| P2 | Typography scale único para breadcrumbs vs títulos |

## Referencias código

- `wwwroot/css/mf-experience-system.css`
- `wwwroot/js/mf-shell-experience.js` (tema + Ctrl+K)
- `Views/Shared/_CommandPalette.cshtml`

---

## Iteración 2026-05-11 (continuación)

| Cambio | Detalle |
|--------|---------|
| **KPIs Mission Control** | `_StatCard` reescrito estilo `mf-xp-kpi` (borde acento, icono, footer enlace) — sustituye small-box AdminLTE en dashboard |
| **Drill-down** | Enlaces desde tarjetas de citas a `Appointments/Index` con `from`/`to` = hoy y `status` según Scheduled/Confirmed/Cancelled/Completed/NoShow; pacientes inactivos → `estadoActivo=false`; cartera → facturas; pendientes → filtro `InvoiceStatus.Pending` |
| **Paleta Ctrl+K** | Entradas Reportes (`Reports/Appointments`), Configuración, Onboarding |
| **CSS** | `.mf-xp-stat-tile`, hover footer, icono warning en dark mode |
