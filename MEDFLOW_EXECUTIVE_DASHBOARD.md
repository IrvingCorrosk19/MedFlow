# MedFlow — Executive dashboard (Clinic Mission Control)

## Objetivo de producto

Un solo lugar para **operación + dirección**: ocupación, calidad de agenda, crecimiento de pacientes, dinero (según permiso `billing.view`), y **siguiente acción** (Growth AI + recuperación).

## Componentes principales

| Componente | Rol |
|------------|-----|
| `_MissionControlHeader` | Hero + proyección financiera MTD (si aplica) |
| `_MissionControlCompareStrip` | **Nuevo:** comparativas rápidas + forecast strip |
| `_GrowthAiInsights` | Reglas sobre KPIs + enlaces IA / recuperación |
| Toolbar | CSV, impresión, Auto 3 min, KPIs API (`mf-mission-control-refresh.js`) |
| Stat cards + Chart.js | Series período seleccionado |

## Métricas nuevas (franja comparativa)

- **Crecimiento pacientes:** mes actual vs anterior (series `NewPatientsByMonth`).
- **Ocupación día:** % completadas sobre citas hoy.
- **Riesgo no-show / cancelación hoy:** % sobre citas hoy (contexto operativo del día).
- **Forecast:** extrapolación lineal facturación mes + enlaces si hay permiso financiero.

## Permisos

- KPIs financieros del hero y strip inferior siguen `ViewBag.ShowFinancialDashboard` / `billing.view`.

---

*Export PDF nativo sigue en backlog analítico; CSV ya disponible.*
