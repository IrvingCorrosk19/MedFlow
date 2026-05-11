# MedFlow — Stickiness y retención (análisis accionable)

**Fuentes:** `MEDFLOW_QUE_FALTA_PARA_SER_EL_MEJOR.md` §7, `MEDFLOW_FUNCIONALIDADES_QUE_GENERAN_DINERO.md`, `MEDFLOW_MODULOS_EXISTENTES.md`, `MEDFLOW_EXPERIENCIA_CLIENTE.md`, `ANALISIS_SUPREMO_SISTEMA.md`, `QA_RESULTADOS_COMPLETOS.md`.

---

## 1. Fuentes de dependencia real (alto stickiness)

| Palanca | Por qué duele migrar | Estado en MedFlow |
|---------|----------------------|-------------------|
| **Historia financiera + facturas + pagos** | Datos y procesos contables acoplados | Modelo amplio dominio + billing QA |
| **Workflows en producción** (N8n / triggers) | Reimplementar reglas fuera | Motor workflow + ejecuciones |
| **Portal paciente en operación diaria** | Pacientes acostumbrados + marca | Dual rutas — **riesgo** si confusión soporte |
| **Integraciones de cobro** | Conciliación y hábitos | Webhooks Stripe / APIs |
| **Datos históricos reporting** | Imposible clonar valor rápido | Dashboard + reportes |

---

## 2. Fuentes de fragilidad (bajo stickiness)

| Problema | Evidencia |
|----------|-----------|
| **UI genérica** — poco orgullo de marca del cliente | AdminLTE (`ANALISIS_SUPREMO`, `MEDFLOW_EXPERIENCIA_CLIENTE`) |
| **Dual portal** — soporte y bugs percibidos | `MEDFLOW_MODULOS_EXISTENTES` |
| **Sin CI/CD** — percepción “vendor informal” | `MEDFLOW_ESTADO_ACTUAL_REAL` |
| **Copilot sin endurecer** — miedo legal si incidente | `MEDFLOW_AI_OPPORTUNITIES` |
| **Paridad con alternativas “buenas suficientes”** si solo CRUD | Diferenciador debe ser **automatización + caja + IA** |

---

## 3. Estrategias de retención (ordenadas por ROI)

### A. Profundizar datos y hábitos

1. **Reporting histórico exportable** (Excel/PDF serios) — migración dolorosa.
2. **Benchmark opt-in** — más valor cuanto más tiempo en plataforma (`MEDFLOW_ROADMAP_PRIORIZADO`).
3. **Auditoría y logs** como narrativa compliance para director — switching cost psicológico.

### B. Automatización como lock-in ético

- Plantillas sector + ejecuciones medibles — “si sales, pierdes las reglas”.
- Documentar **atribución** recuperación ingresos (producto ya orientado a workflows — comunicarlo).

### C. Portal paciente como front-door

- Unificar journey; recordatorios y autocita integrados — hábito paciente (`MEDFLOW_EXPERIENCIA_CLIENTE`).

### D. IA con valor acumulativo

- Insights reconocidos en el tiempo + acciones tomadas — historial que no existe en Excel (`MEDFLOW_AI_OPPORTUNITIES` §3 telemetría).

### E. Partners / grupos

- API móvil + white-label — MDM y contractual lock (`MEDFLOW_MODULOS_EXISTENTES` APIs).

---

## 4. Métricas de retención a instrumentar

| Métrica | Acción si falla |
|---------|-----------------|
| WAU staff por tenant | Customer success |
| % citas con recordatorio workflow activo | Habilitar plantillas |
| Días desde último login director | Re-engagement |
| Churn reason taxonomy | Product discovery |

---

## 5. Lo que NO aumenta stickiness solo

- Más pantallas CRUD sin datos históricos ni automatización.
- IA cosmética sin auditoría — miedo **reduce** retención (`MEDFLOW_AI_OPPORTUNITIES`).

---

*Emparejar con contratos anuales y límites de export masivo en tier bajo (ética + legal primero).*
