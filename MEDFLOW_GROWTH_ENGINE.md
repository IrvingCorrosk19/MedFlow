# MedFlow Growth Engine

**Definición:** Capacidades que **miden, predicen y ejecutan** acciones que aumentan ingresos y reducen fugas — no reportes pasivos.

**Alineación:** `MEDFLOW_FUNCIONALIDADES_QUE_GENERAN_DINERO.md`, `MEDFLOW_OPORTUNIDADES_BILLONARIAS.md`, `MEDFLOW_AI_OPPORTUNITIES.md`.

---

## Capas del motor

| Capa | Qué hace | Ejemplo |
|------|-----------|---------|
| **Datos** | Unificar citas, pagos, ausencias, abandono | Warehouse / vistas materializadas por tenant |
| **Inteligencia** | Scores, cohortes, pronósticos | No-show score, LTV, churn paciente |
| **Acción** | Workflows, mensajes, re-agenda | WhatsApp + email + SMS según preferencia |
| **Feedback** | Cierre de loop en KPIs | “Campaña X recuperó $Y” |

---

## Productos dentro del Growth Engine

1. **Mission Control (CEO)** — un solo lugar para dinero, ocupación, alertas (ver fase 2 del mandato).
2. **Revenue Recovery** — motor dedicado (documento hermano).
3. **CRM médico** — segmentación + journeys legales.
4. **AI Growth** — copilot contextual que **recomienda** y **dispara** (con permisos).

---

## Métricas North Star (clínica)

- **Ingreso neto por hora clínica** (no solo facturación bruta).
- **Utilización de agenda** vs **horas muertas**.
- **Tasa de recuperación** de cartera y de citas perdidas.
- **Coste por paciente adquirido/retenido** vía canales MedFlow.

---

## Go-to-market interno

- Empaquetar como **“Growth”** o **“Plus”** en pricing (no listar 40 features).
- Demos con **antes/después** en mock data anonimizada.

---

## Dependencias técnicas

- Event bus / outbox ya existente o a fortalecer para triggers.
- Feature flags por plan (`AIRecoveryEnabled`, etc.).
- Observabilidad: funnel de campañas y conversiones por paso.

---

*Este documento define el “qué”; implementación por épicas en `MEDFLOW_ENTERPRISE_ROADMAP.md`.*
