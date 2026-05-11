# MedFlow AI Transformation

**Mandato:** IA que **genera dinero y reduce riesgo operativo** — no chat decorativo.

**Referencias:** `MEDFLOW_AI_OPPORTUNITIES.md`, `MEDFLOW_FUNCIONALIDADES_QUE_GENERAN_DINERO.md`, estado real en `MEDFLOW_ESTADO_ACTUAL_REAL.md`.

---

## Principios

1. **Grounding:** respuestas accionables solo con datos del tenant (RAG / tools), no alucinar pacientes.
2. **Roles:** médico vs admin vs recepción — distinto permiso y tono.
3. **Audit:** prompts/resúmenes sensibles loggeados con hash de política.
4. **Clínica:** nunca “prescripción” por IA sin validación humana donde la ley lo exija.

---

## Productos IA (mapa)

| Producto | Valor | Entrada típica |
|----------|-------|----------------|
| **Copilot contextual** | “Qué hago ahora” en pantalla | Vista actual + permisos |
| **Financial Assistant** | Explicar fugas de ingreso | Facturación, cartera, KPI |
| **Growth Assistant** | Campañas y segmentos sugeridos | CRM + agenda |
| **Scheduling Optimizer** | Slots, equipos, sobre-booking controlado | Reglas clínica |
| **No-show predictor** | Priorizar confirmación | Historial + canal |
| **Revenue recovery brain** | Mensaje óptimo + momento | Eventos + políticas |
| **Forecasting** | Proyección 30/90 días | Series históricas tenant |

---

## Integración UX

- Entrada desde **Command Palette** (ruta `AI` / AIDashboard ya enlazada en paleta v0).
- **Insight cards** en Mission Control: texto corto + acción (“Abrir agenda nocturna”).

---

## Coste y control

- **Token budget** por tenant/plan; degradación elegante al superar cuota.
- **Cache** de embeddings y resúmenes por paciente con TTL y revocación.

---

## Entregables de ingeniería (orden sugerido)

1. Capa **tool-calling** unificada (lectura agenda, facturas, paciente activo).
2. **Eval set** interno: preguntas frecuentes admin + casos edge privacidad.
3. Embeddings solo donde el ROI supere coste (empezar por KB clínica/admin).

---

*Revisar trimestralmente contra competencia (`MEDFLOW_ANALISIS_COMPETITIVO.md`).*
