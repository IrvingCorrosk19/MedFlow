# MedFlow — Handoff para continuar (transformación + v2)

**Última sesión:** 2026-05-10  
**Propósito de este archivo:** que mañana sepas **en 2 minutos** qué está hecho, qué tocar primero y dónde está el código.

---

## 1. Estado en una frase

Las **10 fases** quedan en **100% del alcance v2 declarado en repo**: UX (`mf-xp` también en reportes Financial/Patients/Doctors/Appointments), Mission Control + KPIs API, Growth Engine + copiloto, recovery workflows + heurística + **CSV de atribución**, CRM segmentos, redirects portal canónico, plantillas n8n, consola SaaS tenant, checklist seguridad, PWA/SW mínimo. Lo que sigue siendo **fuera de “100% código”** es **QA regresivo documentado**, **roadmap élite** y **decisiones de negocio** (WhatsApp legal, campañas persistidas, etc.).

---

## 2. Cómo retomar mañana (checklist rápido)

1. `dotnet build src/MedFlow.Web/MedFlow.Web.csproj`
2. Subir la app y validar **Mission Control** (`/Dashboard`): toggles **Auto 3 min** vs **KPIs API**.
3. Probar **admin con `settings.manage`**: `/ClinicConsole`, `/SecurityPosture`.
4. Probar **IA + Growth**: `/AI/GrowthEngine` (copiloto embebido).
5. Ejecutar flujos críticos según **`PRUEBAS_FLUJOS_PRIORITARIOS_CLINICAS.md`** y anotar resultados en **`QA_RESULTADOS_COMPLETOS.md`**.

---

## 3. Mapa “qué quedó en código” (por fase)

| # | Fase | Entregable principal | Dónde mirar | v2 |
|---|------|----------------------|-------------|-----|
| 1 | Experience | `mf-xp-*`, empty states, reportes | `mf-experience-system.css`, `Patients/Index`, `Appointments/Index`, `BillingInvoices/Index`, `Views/Reports/*.cshtml`, `_MfExperienceEmptyState` | 100% |
| 2 | Mission Control | JSON KPI + refresh | `DashboardController.KpiSnapshot`, `mf-mission-control-refresh.js`, `_MissionControlHeader` (`data-mf-kpi`) | 100% |
| 3 | AI Growth | Motor + copiloto | `Areas/AI/GrowthEngine`, `_GrowthCopilotEmbed.cshtml`, `CopilotController` | 100% |
| 4 | Revenue | Workflows + heurística + CSV | `RevenueRecoveryController` (`ExportWorkflowMetricsCsv`), `CountSucceededByEventTypesSinceAsync` | 100% |
| 5 | CRM | Segmentos + ranking | `GrowthCrm/Segments`, `IGrowthCrmAnalyticsService` | 100% |
| 6 | Portal | Redirects legacy | `PatientPortalCanonicalMiddleware`, `AccountController` → `/portal/dashboard` | 100% |
| 7 | Automatización | Plantillas JSON | `wwwroot/workflow-templates/*`, `Automations/Index` | 100% |
| 8 | SaaS tenant | Uso vs plan | `ClinicConsoleController`, `TenantUsageDto` | 100% |
| 9 | Seguridad | Checklist admin | `SecurityPostureController` | 100% |
| 10 | PWA | SW mínimo | `service-worker.js`, `_AdminLayout` registro SW | 100% |

---

## 4. Rutas útiles (copiar/pegar)

| URL | Notas |
|-----|--------|
| `/Dashboard` | Mission Control |
| `/Dashboard/KpiSnapshot?days=14` | JSON (requiere sesión + permiso dashboard) |
| `/AI/GrowthEngine` | Growth + copiloto |
| `/RevenueRecovery` | Recovery + atribución workflows |
| `/RevenueRecovery/ExportWorkflowMetricsCsv` | CSV atribución (30 d, por tipo de evento) |
| `/GrowthCrm/Segments` | CRM segmentos + top citas |
| `/ClinicConsole` | Requiere **Configuración** (`settings.manage`) |
| `/SecurityPosture` | Igual |
| `/Automations` | Descarga plantillas workflow |

---

## 5. Prioridades sugeridas para la siguiente sesión

Orden recomendado (ajusta según negocio):

1. **QA regresivo** multi-rol usando los dos MD de pruebas del repo (evitar regresiones en portal y billing).
2. **Mission Control:** validar que **KPIs API** y gráficos Chart.js no divergen en significado (solo hero actualiza en modo fetch).
3. **Portal paciente:** decidir si el siguiente paso es **fusionar vistas** área `PatientPortal` vs rutas `/portal` (trabajo grande; documentar decisiones).
4. **CRM:** persistir scoring / campañas solo si hay modelo comercial claro.
5. **Integraciones:** WhatsApp/SMS con proveedor + **opt-in** legal (fuera de código puro).

---

## 6. Pendientes conscientes (no olvidar)

- **Copiloto / IA:** depende de configuración LLM del tenant (`IAIModelProvider`, límites diarios en procesador).
- **Heurística 12% recovery:** orientativa; no es contabilidad ni promesa financiera.
- **Service worker:** cache mínimo; ampliar con precaución (invalidación de versión).
- **Tests automatizados anti–tenant leak:** aún no sustituyen revisión manual de endpoints nuevos.

---

## 7. Documentos del proyecto a mantener alineados

| Archivo | Uso |
|---------|-----|
| `QA_RESULTADOS_COMPLETOS.md` | Resultados de pruebas |
| `PRUEBAS_FLUJOS_PRIORITARIOS_CLINICAS.md` | Guía de flujos |
| `ANALISIS_FALTANTES_MODULO_A_MODULO.md` | Backlog analítico por módulo |
| `ANALISIS_SUPREMO_SISTEMA.md` | Visión global |
| Paquete `MEDFLOW_WORLDCLASS_*.md` | Estrategia / narrativa comercial |

---

## 8. Nota para quien continúe

Si solo haces **una cosa** mañana: **correr build + flujo admin Mission Control + Growth Engine + Recuperación ingresos**, y anotar fallos en `QA_RESULTADOS_COMPLETOS.md`.

---

*Fin del handoff — actualizar la fecha de sesión al cerrar el día siguiente.*
