# MedFlow Enterprise Roadmap (priorizado)

**Entrada:** `MEDFLOW_ROADMAP_PRIORIZADO.md`, `ANALISIS_SUPREMO_SISTEMA.md`, `ANALISIS_FALTANTES_MODULO_A_MODULO.md`, `QA_RESULTADOS_COMPLETOS.md`.

---

## Horizontes

### H1 — 0–90 días (fundamentos que venden)

| # | Entrega | Por qué |
|---|---------|---------|
| 1 | **Experience System** en todo el shell admin | Ya iniciado (CSS + paleta + dark) |
| 2 | **Mission Control v1** — KPI ingresos, ocupación, cancelaciones | Narrativa “crecimiento” |
| 3 | **Recuperación ingresos v1** — facturas + recordatorios | ROI directo |
| 4 | **Unificación portal** — una experiencia | NPS paciente |
| 5 | **Seguridad** — rate limit APIs públicas, revisión OWASP top issues | Enterprise |

### H2 — 90–180 días (IA + automatización)

| # | Entrega |
|---|---------|
| 6 | AI Copilot **con tools** (lectura datos tenant) |
| 7 | No-show predictor + acciones |
| 8 | CRM segmentos + campañas **con opt-in** |
| 9 | n8n / webhooks ampliados para enterprise |

### H3 — 180–365 días (escala mundial)

| # | Entrega |
|---|---------|
| 10 | Observabilidad unificada (traces, dashboards SLO) |
| 11 | DR runbooks + backups probados |
| 12 | PWA / push notifications |

---

## Dependencias críticas

- **Datos:** vistas o pipeline para KPI financieros sin pegar DB en cada request.
- **Legal:** mensajes masivos y salud — revisión por mercado.
- **QA:** regresión multi-rol tras cada hito (flujos en `PRUEBAS_FLUJOS_PRIORITARIOS_CLINICAS.md`).

---

## Riesgos

- Construir IA sin eval → reputación.
- Campañas sin límites → spam y churn.

---

*Reordenar solo con datos de uso o revenue at risk.*
