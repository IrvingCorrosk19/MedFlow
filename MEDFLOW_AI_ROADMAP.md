# MedFlow AI — roadmap (copilot contextual + gobernanza)

**Versión:** 1.0 · **Fecha:** 2026-05-10  
**Contexto:** `ANALISIS_SUPREMO_SISTEMA.md` (IA ~6/10; insights/copilot existentes pero no omnipresentes).

---

## Principios (AI platform + healthcare expert)

1. **Útil o no existe:** ninguna feature IA solo “marketing”; debe medir tiempo ahorrado o errores evitados.
2. **Trazabilidad:** sugerencias clínicas/administrativas con referencia a datos del sistema (IDs, fechas), no solo texto libre.
3. **Human-in-the-loop:** decisiones sensibles (signos vitales críticos, dosis) no autónomas sin política explícita del cliente/región.
4. **Evaluación:** datasets de prueba y métricas offline antes de subir prompts a producción.
5. **Costo y latencia:** caching de embeddings/resúmenes por tenant; rate limits por plan SaaS.

---

## Capas de producto IA

| Capa | Descripción | Ejemplo |
|------|-------------|---------|
| **L0 — Insights batch** | Jobs/analytics ya existentes | Resúmenes operativos diarios |
| **L1 — Asistente reactivo** | Chat con tools sobre APIs internas | “¿Cuánto cartera > 30 días?” |
| **L2 — Copilot contextual** | Panel lateral según ruta/paciente/cita | Resumen visita + próximos pasos |
| **L3 — Proactivo** | Alertas sugeridas antes de que el usuario pregunte | Pico cancelaciones + acciones |
| **L4 — Predicción** | Forecast demanda, no-show | Planificación agenda |

**Meta 24 meses:** L2 maduro en 3 módulos + L3 en operaciones; L4 selectivo.

---

## Roadmap por trimestre (indicativo)

### Q1–Q2

- Inventario de endpoints seguros para tool-calling (paciente, cita, factura **scoped tenant**).
- Plantillas de prompt versionadas en repo + feature flags por tenant/plan.
- “Resumen paciente” generado desde datos estructurados (no solo texto libre).

### Q3–Q4

- Copilot en expediente: completar texto asistido con snippets auditados.
- Billing assistant: explicar saldos y próximos pasos de cobro (solo datos del tenant).

### Año 2

- Detección anomalías operativas (patrones de cancelación, pagos).
- Evaluation harness + regression suite de prompts.

---

## Integración UX

- Panel IA **no** tapa flujo principal; se abre con atajo y recordatorio dismissible.
- Estados: loading, vacío (“sin datos suficientes”), error modelo con retry.

---

## Riesgos y cumplimiento

- **PII** en prompts/logs — minimización, retención corta, opción “modo estricto sin LLM externo”.
- Mercados con normativa específica sobre decisión automatizada — revisión legal caso por caso.

---

## KPIs

- Adopción (% sesiones que abren copilot).
- Tiempo medio en tarea “buscar contexto paciente”.
- Tasa de “thumb up/down” en sugerencias.
- Incidentes corregidos por feedback humano.

---

*Actualizar cuando cambien proveedores de modelo o políticas de datos.*
