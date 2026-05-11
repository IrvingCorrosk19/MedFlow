# MedFlow — Mapa de módulos existentes (repositorio)

**Metodología:** Cruce de `ANALISIS_FALTANTES_MODULO_A_MODULO.md` (§ numerados), áreas bajo `src/MedFlow.Web`, lista de controladores, dominio en `MedFlow.Domain`.

**Leyenda de estado**

- **En código:** hay controladores/vistas/servicios dedicados.
- **Madurez:** “alta” = flujo principal usable según QA parcial; “media” = existe pero con deuda en doc faltantes; “variable” = depende del tenant/plan.

---

## Staff / clínica (Razor principal)

| Módulo | En código | Evidencia principal | Madurez percibida |
|--------|-----------|----------------------|-------------------|
| **Dashboard ejecutivo** | Sí | `DashboardController`, JS ejecutivo, KPIs/gráficos | Alta con deuda UX/export (`ANALISIS_FALTANTES` §1) |
| **Pacientes** | Sí | `PatientsController`, CSV import/export, paginación (evolución documentada en tabla sync + trabajo reciente) | Alta |
| **Doctores** | Sí | `DoctorsController`, permisos | Media |
| **Citas** | Sí | `AppointmentsController`, estados, calendario/API | Media–alta |
| **Expediente médico** | Sí | `MedicalRecordsController`, adjuntos, búsqueda | Media–alta |
| **Recetas** | Sí | `PrescriptionsController`, entidad `Prescription` | Alta tras fixes QA (migración) |
| **Facturas** | Sí | `BillingInvoicesController` | Alta |
| **Pagos** | Sí | `PaymentsController` | Media–alta |
| **Caja** | Sí | `CashMovementsController` | Media |
| **Reportes** | Sí | `ReportsController` | Media |
| **Analytics** | Sí | `AnalyticsController`, exports API | Media |
| **Event logs / auditoría UI** | Sí | `EventLogsController`, `SecurityAuditController` | Variable |
| **Notificaciones (staff)** | Sí | APIs/templates | Media |
| **Plantillas de notificación** | Sí | `NotificationTemplatesController` | Media |
| **Configuración** | Sí | `SettingsController`, impuestos `TaxRatesController` | Media |
| **Onboarding tenant** | Sí | `OnboardingController` | Media con deuda validación (`ANALISIS_FALTANTES` §24) |
| **Branding comercial tenant** | Sí | `CommercialController` (nombre inferido de lista controladores) | Variable |
| **Cuentas bancarias / ledger** | Sí | `BankAccountsController`, `LedgerController`, `JournalEntriesController`, `ChartOfAccountsController`, `FiscalPeriodsController` | Media (módulo contable formal) |
| **Administración usuarios** | Sí | `AdminUsersController` | Alta en QA |
| **Roles/permisos** | Sí | `PermissionsController` + servicios | Alta |
| **Automatizaciones** | Sí | `AutomationsController`, dominio `WorkflowDefinition`/`WorkflowExecution` | Media |
| **Ejecuciones workflow** | Sí | `WorkflowExecutionsController` | Media |
| **Dos factores** | Sí | `TwoFactorController` | Variable |
| **Cuenta / login staff** | Sí | `AccountController` | Alta en QA |

---

## Portal del paciente (dual)

| Superficie | Ruta / área | Notas |
|------------|-------------|--------|
| **Área PatientPortal** | `/PatientPortal/*` | Layout dedicado, citas, facturas, perfil, notificaciones (`Areas/PatientPortal`) |
| **Controlador “portal” legacy/alternativo** | `/portal/*` | `PatientPortalController` en `Controllers/` — vistas bajo `Views/PatientPortal/` |

**Estado:** ambas superficies coexisten; implica **duplicación de UX y mantenimiento** — riesgo producto (`ANALISIS_FALTANTES` §16–20 + código).

---

## Plataforma SaaS (SuperAdmin)

| Módulo | En código |
|--------|-----------|
| **Tenants** | `Areas/SuperAdmin/Controllers/TenantsController` |
| **Planes / suscripciones** | `PlansController`, `SubscriptionsController` |
| **Billing SaaS** | `Areas/SuperAdmin/Controllers/BillingController` + Stripe en dominio |
| **Home SuperAdmin** | `HomeController` |

---

## Operaciones / plataforma interna

| Módulo | En código |
|--------|-----------|
| **Ops — salud workers / webhooks** | `Areas/Ops` — `HealthDashboardController`, `WorkersController`, `WebhooksController` |
| **Health app** | `HealthController` |

---

## IA (área dedicada)

| Pieza | En código |
|-------|-----------|
| **Copilot** | `Areas/AI/Controllers/CopilotController` |
| **Insights** | `InsightsController` |
| **Dashboard IA** | `AIDashboardController` |
| **Configuración IA** | `AISettingsController` |
| **Recomendaciones** | `RecommendationsController` |
| **Dominio** | `AIInsight`, enums, `AISettingsKeys` |
| **Servicios** | Interfaces `IOperationalCopilotService`, `IAIInsightService`, proveedores, riesgo no-show, engagement, etc. |

**Deuda:** XSS/spinner/rate limit en Copilot; validación filtros Insights; integración visual con dashboard ejecutivo (`ANALISIS_FALTANTES` §21–23).

---

## APIs

| API | En código |
|-----|-----------|
| **Móvil V1** | `Controllers/Api/Mobile/V1/*` — auth, citas, pagos, notificaciones, push |
| **Staff/tenant** | `TenantStaffAuthController`, búsqueda global `GlobalSearchController` |
| **Analytics export** | `AnalyticsExportController` |
| **Webhooks** | `StripeWebhookController`, `N8nWebhooksController` |
| **Paciente vitales** | `PatientVitalsController` (gráficos en ficha paciente) |

---

## Dominio (macro)

Entidades representativas en `MedFlow.Domain/Entities`: **Tenant**, **Patient**, **Appointment**, **MedicalRecord**, **Prescription**, **BillingInvoice**, **Payment**, **CashMovement**, **Notification**, **Workflow***, **JournalEntry**, **SaaSInvoice**, **SubscriptionPlan**, **AIInsight**, etc.

---

## Resumen

**MedFlow no es “un CRUD de citas”:** es una **plataforma** con **operación clínica**, **tesorería**, **contabilidad opcional**, **SaaS**, **automatización**, **IA**, **portal paciente** y **API móvil**. La complejidad ya es de **producto maduro en construcción**, con **deuda documentada** por módulo.

---

*Para profundidad por huecos, usar `ANALISIS_FALTANTES_MODULO_A_MODULO.md` sección por sección y validar contra el branch actual.*
