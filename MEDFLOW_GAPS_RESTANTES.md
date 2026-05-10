# MedFlow — gaps restantes (sincronizado con evidencia de repo)

**Versión:** 1.0 · **Fecha:** 2026-05-10  
**Fuentes:** `ANALISIS_FALTANTES_MODULO_A_MODULO.md` (histórico + tabla 2026-05-10), `ANALISIS_SUPREMO_SISTEMA.md`, `QA_RESULTADOS_COMPLETOS.md`, `PRUEBAS_FLUJOS_PRIORITARIOS_CLINICAS.md`

---

## Cómo leer este documento

- Las **secciones numeradas largas** del archivo `ANALISIS_FALTANTES_MODULO_A_MODULO.md` conservan texto del **2026-04-02**. Muchos bullets **ya no aplican** (ej. permisos en Pacientes/Doctores, export CSV pacientes, try/catch dashboard).  
- **No usar** ese documento como backlog literal sin cruzar con código.  
- Esta lista prioriza **brechas verificables** o **de alto impacto** alineadas con las fuentes anteriores.

---

## Cerrado recientemente (no priorizar de nuevo como P0)

| Tema | Evidencia |
|------|-----------|
| KPIs financieros en `/` sin permiso | `billing.view` + vista/dashboard (`QA_RESULTADOS_COMPLETOS.md`) |
| DataTables i18n CORS | JSON local `/lib/datatables/es-ES.json` |
| Columnas recetas / 500 | Migración `SyncPrescriptionColumnsWithDomain` |
| Login paciente vs staff | `AccountController` redirect portal |
| Pacientes: búsqueda, filtros, CSV, SetActive, delete con reglas | Código + tabla estado en `ANALISIS_FALTANTES_*` cabecera |
| Dashboard null-safe y días 7–90 | `DashboardController` + vista |

---

## Gaps críticos (P0 / seguridad / continuidad)

| ID | Gap | Por qué es crítico | Acción |
|----|-----|-------------------|--------|
| G-SEC-01 | **Auditoría completa** de autorización en **todos** los endpoints (MVC + API + áreas) | Un solo GET olvidado puede exponer PHI entre roles/tenants | Parcial 2026-05-10: APIs staff-only (`GlobalSearch`, `NavNotifications`, `PatientVitals`) + alcance médico en vitales/bell; seguir revisando MVC/API |
| G-SEC-02 | **Config producción** (CORS, rate limit, secrets) no debe copiar flags QA (`AllowOperationsWhenPastDue`, rate limit off) | Riesgo operativo y abuso | Checklist despliegue + secret store |
| G-SEC-03 | **Uploads** — validación MIME profunda, cuotas, path traversal | OWASP file upload | Servicio unificado de upload |
| G-QA-01 | Pruebas **POST** end-to-end insuficientes (`QA_RESULTADOS_COMPLETOS.md`) | Regresiones en crear/editar/pagar | Extender script HTTP o Playwright según `PRUEBAS_FLUJOS_*` |
| G-TEN-01 | **Aislamiento tenant** — disciplina en filtros EF + SuperAdmin | Regresión = incidente máximo | Tests aislamiento ampliados + revisión queries nuevas |

---

## Gaps altos (P1 — producto clínico y tesorería)

| ID | Gap | Fuente |
|----|-----|--------|
| G-P1-01 | Flujos citas: calendario visual, slots, conflictos UX claros | `ANALISIS_FALTANTES_*` §4 + supremo UX |
| G-P1-02 | Expediente: adjuntos MIME reales, eliminar adjunto, límites tamaño | `ANALISIS_FALTANTES_*` §5 |
| G-P1-03 | Facturación/pagos: drill-down ejecutivo y exports PDF “bonitos” | Supremo + QA matriz F |
| G-P1-04 | IA: hoy insights/copilot no son omnipresentes ni evaluados | `ANALISIS_SUPREMO_*` |
| G-P1-05 | Portal paciente: experiencia “app premium”, PDFs, notificaciones RT | Petición producto + gaps históricos portal |

---

## Gaps medios (P2 — dirección, reporting, WOW)

- Dashboard: rango `from`/`to`, caché, impresión charts, integración panel IA.
- Reportes avanzados, NPS, CRM salud lite.
- Onboarding mágico y time-to-value medido.
- Dark mode **persistente** (capa CSS preparada en `medflow-premium.css` con `data-theme="dark"` — falta UI toggle y preferencias).

---

## Gaps bajos / largo plazo (P3)

- Contabilidad formal completa para todos los tenants.
- Automatizaciones/workflows masivos.
- FHIR / interoperabilidad como producto.
- DevOps “enterprise”: pipelines, staging gates, DR documentado — **Supremo** ya señaló ausencia de `.github/workflows` en auditoría.

---

## Deuda documental

- Mantener **una** fuente de verdad de backlog: este archivo + issues tracker; reducir duplicación con el análisis histórico por módulo.

---

*Actualizar este archivo tras cada release mayor o auditoría de seguridad.*
