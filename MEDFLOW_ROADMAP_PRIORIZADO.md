# MedFlow — Roadmap priorizado (auditoría + negocio + riesgo)

**Principios:** maximizar **ROI clínico**, **ARR MedFlow**, **reducción riesgo**, **habilitar upsell**. Basado en `PRUEBAS_FLUJOS_PRIORITARIOS_CLINICAS.md` (P0–P3), `QA_RESULTADOS_COMPLETOS.md`, `ANALISIS_FALTANTES_MODULO_A_MODULO.md`, `ANALISIS_SUPREMO_SISTEMA.md`.

---

## Horizonte 0–90 días — **Confianza + ingresos percibidos**

| # | Iniciativa | Prioridad | Por qué |
|---|------------|-----------|---------|
| 1 | **CI/CD** (build, test, migraciones gated) | P0 | Sin esto, escala equipo se rompe |
| 2 | **Copilot:** XSS + límites + errores UX | P0 | Riesgo seguridad/reputación (`§21`) |
| 3 | **CORS prod** checklist | P0 | Riesgo supremo |
| 4 | **Dashboard exports** reales donde siguen disabled | P1 | Ventas dirección |
| 5 | **Insights filtros** validados + export | P1 | Monetización IA futura |
| 6 | **Regresión automatizada** extender POST críticos | P1 | QA doc pide ampliar |
| 7 | **Unificar estrategia portal paciente** (decisión producto) | P1 | Reduce soporte |

---

## Horizonte 3–9 meses — **Premium + enterprise-lite**

| # | Iniciativa | Por qué |
|---|------------|---------|
| 1 | **Design system** + refresh shell principal | Percepción premium |
| 2 | **Observabilidad obligatoria** staging/prod OTLP + alertas | Enterprise sales |
| 3 | **API developer portal** + API keys por tier | Modelo Stripe-like |
| 4 | **Integración IA ↔ dashboard ejecutivo** | Story ROI |
| 5 | **Onboarding:** validaciones prometidas en UI | Conversión trial |
| 6 | **SSO** (si ICP enterprise regional) | Deal size |

---

## Horizonte 9–24 meses — **Plataforma**

| # | Iniciativa | Por qué |
|---|------------|---------|
| 1 | **FHIR read/write selectivo** o partner integration hub | Expansión TAM |
| 2 | **Marketplace conectores certificados** | Efecto red |
| 3 | **Telemedicina** si vertical elegido lo exige | ARPU |
| 4 | **Benchmarking anonimizado opt-in** | Upsell datos |

---

## Priorización por **flujo clínico** (del documento de pruebas)

| Prioridad doc | Refuerzo roadmap |
|---------------|------------------|
| **P0** citas/pacientes/auth | Smoke siempre verde + datos seed limpios |
| **P1** historia/recetas/factura/pago | Suite ampliada POST |
| **P2** dashboard/reportes/portal admin | Polish y permisos |
| **P3** contabilidad/workflows/SaaS admin | Cuando touch module |

---

## Métricas de éxito sugeridas (producto)

| Métrica | Objetivo orientativo |
|---------|------------------------|
| Time-to-first-value onboarding | ↓ |
| No-show rate tenant | ↓ vs baseline |
| Días saldo outstanding | ↓ |
| NPS staff recepción | ↑ |
| Uptime | 99.9% aspiracional con ops |
| Migraciones fallidas deploy | 0 |

---

## Dependencias / riesgos

- **Legal** por país antes telemedicina y datos agregados.
- **No prometer** FHIR/enterprise hospital sin equipo dedicado.

---

## Conclusión

Este roadmap **balancea** deuda documentada, **riesgo** (Copilot/CORS), **monetización** (exports, IA, API), y **percepción** (UI). Debe revisarse **trimestralmente** con ventas y soporte.

---

*Versión 2026-05-10 · sincronizar con backlog en herramienta PM.*
