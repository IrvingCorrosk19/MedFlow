# MedFlow — Funcionalidades que ya “pegan” como premium (o pueden venderse como tal)

**Definición operativa de “premium” aquí:** (a) **diferenciación clara** frente a Excel/agenda suelta, (b) **valor percibido por rol**, (c) **base técnica real en código**, no slogans.

**Fuentes:** `ANALISIS_SUPREMO_SISTEMA.md`, `ANALISIS_FALTANTES_MODULO_A_MODULO.md`, `QA_RESULTADOS_COMPLETOS.md`, exploración de `src/`.

---

## 1. Ya premium o “tier alto” (con matices)

| Funcionalidad | Por qué se siente premium / enterprise-lite | Matiz honesto |
|---------------|-----------------------------------------------|----------------|
| **Multi-tenant SaaS real** | Aislamiento por tenant, planes, Stripe, facturación plataforma | Requiere disciplina y tests anti-fuga continuos |
| **Facturación clínica + pagos + saldos** | Modelo financiero completo en dominio; permisos billing | UX/pdf/excel aún con deuda en varios listados |
| **Dashboard ejecutivo con KPIs y gráficos** | Narrativa de dirección; CSV; bloque financiero por permiso (`billing.view`) | Falta polish “McKinsey dashboard” y exports PDF/Excel top |
| **Automatización / workflows** | Motor de definiciones y ejecuciones; integración N8n | No es Zapier público; requiere conocimiento técnico |
| **IA operativa (Insights + Copilot)** | Diferenciador en SMB; interfaces de proveedor y procesamiento | Copilot necesita hardening (XSS, rate limit) según doc faltantes |
| **Analytics y benchmarking** (interfaces) | `IBenchmarkingService`, analytics avanzado, exports | Valor = calidad datos + UX consumo |
| **Portal del paciente** | Doble superficie + opciones por tenant; reduce llamadas | Duplicidad `/portal` vs área; branding parcial |
| **API móvil JWT** | Habilita apps nativas / socios | No sustituye app pulida sin cliente dedicado |
| **Observabilidad preparada** | OpenTelemetry, health | Valor en prod solo si se despliega OTLP + alertas |
| **SuperAdmin** | Gestión tenants, billing SaaS | Falta pulir exports/filtros avanzados (doc faltantes) |

---

## 2. Premium **potencial** (base existe; falta empaque comercial)

| Funcionalidad | Qué falta para cobrar caro sin fricción |
|---------------|----------------------------------------|
| **Ledger / períodos fiscales** | UX guiada “para no contadores”, informes regulatorios por país |
| **Workflows** | Plantillas sector vertical + marketplace de triggers |
| **IA Insights** | SLA de precisión, export, bulk acknowledge, integración dashboard principal |
| **Copilot** | Guardrails, auditoría de prompts, planes enterprise |
| **Onboarding** | Validaciones prometidas en UI, trial honesto, AJAX código tenant |
| **White-label fuerte** | Dominio, email transaccional, tema propio más allá de logo/color |

---

## 3. Qué **no** es premium hoy (según `ANALISIS_SUPREMO`)

- **Capa visual por defecto:** AdminLTE + Bootstrap 4 + tablas/DataTables = percepción “software interno”.
- **Experiencia unificada portal:** dos rutas conceptuales para paciente elevan confusión y costo de mantenimiento.
- **Ausencia de CI/CD visible** en repo → percepción enterprise “informal” para compradores grandes.

---

## 4. Empaquetado comercial sugerido (sin implementar aquí)

| Tier sugerido | Contenido alineado al código actual |
|---------------|--------------------------------------|
| **Core** | Citas, pacientes, expediente básico, portal paciente básico |
| **Pro** | Facturación completa, reportes, notificaciones, analytics estándar |
| **Business** | Workflows, exports avanzados, más automatización |
| **Enterprise** | Contabilidad, SLA, API keys dedicadas, SSO (si se construye), soporte prioritario |
| **AI Add-on** | Copilot + Insights gobernados + evaluación |

---

## 5. Conclusión

MedFlow **ya tiene sustancia** para posicionarse como **premium operativo** en SMB/región: **SaaS**, **finanzas**, **automatización**, **IA**, **API móvil**. El **gap premium percibido** está en **capa visual única**, **narrativa de confianza** (compliance), **integraciones vendibles**, y **operación** (CI/CD, métricas).

---

*Ventas: usar este mapa para no prometer FHIR/enterprise hospital sin roadmap explícito.*
