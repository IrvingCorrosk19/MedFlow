# MedFlow — Roadmap “billón” (realista: plataforma regional → global aspiracional)

**Disclaimer:** “Billón” = narrativa de **escala y opción de expansión**, no promesa. Requiere capital, compliance y canal (`MEDFLOW_OPORTUNIDADES_BILLONARIAS` §1).

**Fuentes:** `MEDFLOW_ROADMAP_PRIORIZADO.md`, `MEDFLOW_OPORTUNIDADES_BILLONARIAS.md` §8, `ANALISIS_SUPREMO_SISTEMA.md`, `MEDFLOW_ESTADO_ACTUAL_REAL.md`.

---

## FASE 1 — Confianza + brillo (0–6 meses)

**Objetivo negocio:** cerrar más deals SMB, reducir churn temprano, eliminar riesgos demo.

| Iniciativa | Impacto UX | Impacto ventas | Impacto ARR | Impacto retención |
|------------|------------|----------------|-------------|-------------------|
| CI/CD + gates | — | Alto (credibilidad) | Medio (velocidad equipo) | Alto (menos bugs) |
| Copilot XSS + límites + UX error | Alto | Alto | Add-on IA viable | Alto |
| CORS prod | — | Medio | — | Medio |
| Design system fase 1 + shell | **Muy alto** | **Muy alto** | Medio (pricing) | Alto |
| Portal: decisión canónica + deprecación planificada | Alto | Medio | — | Alto |
| Regresión POST P1 críticos | — | Medio | — | Alto |
| Dashboard exports reales / KPI links | Alto | Alto | Medio | Medio |

**Salida fase:** producto **se ve** 2026; **se vende** sin miedo IA; **se despliega** sin vergüenza DevOps.

---

## FASE 2 — Premium + enterprise-lite (6–18 meses)

**Objetivo:** ARPU ↑, logos multi-sede, partners.

| Iniciativa | Impacto |
|------------|---------|
| Observabilidad obligatoria + SLOs | Enterprise trust |
| API developer portal + API keys por tier | Nuevo canal revenue (`MEDFLOW_ANALISIS_COMPETITIVO` §4) |
| IA ↔ Mission Control integrado | Story ROI (`MEDFLOW_AI_OPPORTUNITIES`) |
| Onboarding trial medido (time-to-value) | Conversión |
| SSO SAML (si ICP) | Deal size |
| Calendario citas + conflictos UX | Operación recepción (`ANALISIS_FALTANTES` §4) |
| Insights export + bulk actions | Monetización IA |

**Salida fase:** MedFlow es **default** para redes regionales elegidas (vertical acotado).

---

## FASE 3 — Plataforma + datos (18–36 meses)

**Objetivo:** efecto red, defensa competitiva.

| Iniciativa | Impacto |
|------------|---------|
| FHIR read-first **o** integration hub partners | TAM expansión (`MEDFLOW_OPORTUNIDADES_BILLONARIAS`) |
| Marketplace conectores certificados | Take rate |
| Benchmark anonimizado opt-in | Datos como producto (`MEDFLOW_ROADMAP_PRIORIZADO`) |
| Telemedicina / lab (si vertical) | ARPU |
| Campañas CRM compliant | Adjacent revenue (`MEDFLOW_FUNCIONALIDADES_QUE_GENERAN_DINERO`) |

**Salida fase:** **ecosistema** empieza a existir; sin governance se paraliza (`MEDFLOW_OPORTUNIDADES_BILLONARIAS` §7).

---

## FASE 4 — Ecosistema global aspiracional (36+ meses)

**Objetivo:** categoría dominante en vertical(es) elegidos; opción estratégica M&A o expansión continental.

| Palanca | Notas |
|---------|------|
| Partner channel + consultoras | Escala sin headcount lineal (`MEDFLOW_QUE_FALTA_PARA_SER_EL_MEJOR`) |
| Cumplimiento multi-país | Legal primero (`MEDFLOW_OPORTUNIDADES_BILLONARIAS` §4) |
| Data residency | Enterprise global |
| Marca separada de “AdminLTE MEDFLOW” | Memoria visual nueva |

**Dependencia crítica:** Fases 1–2 bien ejecutadas; si no, Fase 4 es fantasía (`MEDFLOW_OPORTUNIDADES_BILLONARIAS` §9).

---

## Cruce con métricas éxito (`MEDFLOW_ROADMAP_PRIORIZADO` §56)

- Time-to-first-value onboarding ↓  
- No-show ↓  
- Días outstanding ↓  
- NPS recepción ↑  
- Uptime / MTTR transparentes  
- Migraciones fallidas = 0  

---

## Relación con QA

- Fase 1 debe mantener **37/37** HTTP + tests creciendo; añadir cobertura donde `QA_RESULTADOS` marca huecos P1.

---

*Versión 2026-05 — sincronizar con PM tool y revenue board trimestral.*
