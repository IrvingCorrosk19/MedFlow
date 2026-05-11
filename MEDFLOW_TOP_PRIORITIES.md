# MedFlow — TOP prioridades (por dimensión)

**Método:** Sinergia de `MEDFLOW_ROADMAP_PRIORIZADO.md`, `MEDFLOW_QUE_FALTA_PARA_SER_EL_MEJOR.md`, `ANALISIS_SUPREMO_SISTEMA.md`, `MEDFLOW_AI_OPPORTUNITIES.md`, `ANALISIS_FALTANTES_MODULO_A_MODULO.md`, `QA_RESULTADOS_COMPLETOS.md`.

**Regla:** Impacto × (ventas + retención + riesgo + ARPU).

---

## TOP 10 mejoras (transversales)

1. **CI/CD** con build + tests + migraciones gated — sin esto la escala del equipo se rompe (`MEDFLOW_ROADMAP_PRIORIZADO`, `MEDFLOW_ESTADO_ACTUAL_REAL`).
2. **Copilot:** mitigación XSS, rate limit, UX errores — riesgo reputacional (`MEDFLOW_AI_OPPORTUNITIES` §7).
3. **CORS / headers prod** checklist endurecido (`ANALISIS_SUPREMO`).
4. **Design system propietario** + refresh shell — romper AdminLTE como cara del producto (`ANALISIS_SUPREMO`, `MEDFLOW_EXPERIENCIA_CLIENTE`).
5. **Unificación estrategia portal paciente** — una historia de rutas (`MEDFLOW_MODULOS_EXISTENTES`, `MEDFLOW_EXPERIENCIA_CLIENTE`).
6. **Dashboard drill-down + exports reales** (Excel/PDF donde estén rotos o disabled) — ventas dirección (`ANALISIS_FALTANTES` §1, roadmap).
7. **Insights IA:** filtros validados + export + acercamiento visual al Mission Control (`MEDFLOW_AI_OPPORTUNITIES`, `ANALISIS_FALTANTES` §45 integración).
8. **Regresión HTTP POST** (cita, pago, edición crítica) — `QA_RESULTADOS` gap declarado.
9. **Cerrar GET sin `RequirePermission`** donde el análisis histórico sigue vigente — riesgo datos (`ANALISIS_FALTANTES` Pacientes/Citas/Expediente — validar en branch).
10. **Observabilidad obligatoria** staging/prod (OTel ya en código; falta cultura SLO) (`MEDFLOW_ROADMAP_PRIORIZADO` 3–9 meses).

---

## TOP 10 módulos a potenciar (valor ya alto)

1. **Facturación + pagos + caja** — motor dinero clínica (`MEDFLOW_FUNCIONALIDADES_QUE_GENERAN_DINERO`).
2. **Dashboard ejecutivo** — narrativa dirección; CSV ya; falta polish WOW (`MEDFLOW_FUNCIONALIDADES_PREMIUM`).
3. **Automatizaciones / workflows + N8n** — diferenciador plataforma (`MEDFLOW_MODULOS_EXISTENTES`).
4. **SuperAdmin / tenants / Stripe** — ARR MedFlow (`MEDFLOW_ESTADO_ACTUAL_REAL`).
5. **Portal paciente** — retención + menos llamadas; unificar UX (`MEDFLOW_EXPERIENCIA_CLIENTE`).
6. **Área IA** (Copilot + Insights + riesgos no-show/pago) — upsell y ROI (`MEDFLOW_AI_OPPORTUNITIES`).
7. **API móvil V1** — canal partners / white-label (`MEDFLOW_MODULOS_EXISTENTES`).
8. **Reportes + analytics export** — upgrade Pro/Business (`MEDFLOW_FUNCIONALIDADES_PREMIUM`).
9. **Citas + agenda** — core operación; calendario visual = deuda fuerte (`ANALISIS_FALTANTES` §4).
10. **Contabilidad (ledger)** — enterprise-lite; requiere UX “no contador” (`MEDFLOW_FUNCIONALIDADES_PREMIUM` §2).

---

## TOP 10 quick wins (≤90 días, alto impacto percibido)

1. CI/CD mínimo en main.
2. Copilot guardrails mínimos vendibles.
3. CORS prod documentado y aplicado.
4. Empty states + microcopy en dashboard/listados críticos (`MEDFLOW_EXPERIENCIA_CLIENTE`).
5. KPI dashboard con enlace a módulo (donde falte) (`ANALISIS_FALTANTES` §1).
6. Manifest/PWA ya en QA v2 — mantener y comunicar “instalable”.
7. CSV dashboard y permisos financieros — ya mitigado QA; comunicarlo en sales deck.
8. Plantillas workflow JSON en producto — empaque “starter pack” vertical.
9. Insights: bulk acknowledge / export parcial si backend listo.
10. Documentación OpenAPI stub para `/api` — habilita partners (`MEDFLOW_ANALISIS_COMPETITIVO`).

---

## TOP 10 WOW features (inversión media–alta)

1. **Copilot con citación a datos del tenant** (trazabilidad) — `MEDFLOW_AI_OPPORTUNITIES`, `MEDFLOW_OPORTUNIDADES_BILLONARIAS` §2.
2. **Simulador “impacto en ingresos si no-show baja X%”** — demo killer (`MEDFLOW_AI_OPPORTUNITIES` §5).
3. **Agenda semanal visual + conflictos visibles** — deuda citas (`ANALISIS_FALTANTES` §4).
4. **Vista “mi día” médico** — reducción dispersión (`MEDFLOW_EXPERIENCIA_CLIENTE` §6).
5. **Orquestador clínico-financiero** en UI (episodio → cargo → cobro visible) (`MEDFLOW_ANALISIS_COMPETITIVO` §7).
6. **Command palette** unificado (vs solo búsqueda global) (`ANALISIS_SUPREMO`).
7. **Informes PDF “McKinsey-lite”** por rol — ARPU (`MEDFLOW_FUNCIONALIDADES_PREMIUM`).
8. **Marketplace plantillas workflow** certificadas — efecto red (`MEDFLOW_OPORTUNIDADES_BILLONARIAS` §7).
9. **Benchmarking anonimizado opt-in** — datos como producto (`MEDFLOW_ROADMAP_PRIORIZADO` 9–24 m).
10. **Telemedicina / laboratorio** solo si vertical elegido — no dispersar (`MEDFLOW_QUE_FALTA_PARA_SER_EL_MEJOR`).

---

## TOP 10 mejoras UX

1. Sistema visual propio (tokens, tipografía, densidad).
2. Estados vacíos diseñados (no tabla vacía).
3. Latencia percibida en `/` y listados pesados — budgets Web Vitals (`MEDFLOW_EXPERIENCIA_CLIENTE` §5).
4. Un solo portal paciente.
5. Menos formulario denso en alta paciente/cita — flujos recepción (`MEDFLOW_EXPERIENCIA_CLIENTE`).
6. Jerarquía en tablas (menos ruido).
7. Impresión dashboard con CSS print — hoy frágil (`ANALISIS_FALTANTES` §1).
8. Microcopy clínico empático (menos ERP).
9. Mobile/PWA experiencia coherente con API existente.
10. Accesibilidad como programa (WCAG), no accidente.

---

## TOP 10 mejoras IA

1. XSS + límites Copilot.
2. Rate limit + cost control por tenant.
3. Auditoría de prompts/respuestas para enterprise story.
4. Integración Insights ↔ Mission Control (no módulo aislado).
5. Evaluación/métricas por release de prompts.
6. Human-in-the-loop clínico para sugerencias sensibles.
7. No-show / cobranza como **acciones** en UI, no solo scores.
8. Export insights y filtros validados.
9. Tiering comercial IA (mensajes, severidad, API).
10. Tool-calling sobre APIs internas (patrón OpenAI §14 supremo).

---

## TOP 10 funcionalidades premium (empaquetables ya o casi)

1. **Business tier:** workflows + exports avanzados (`MEDFLOW_FUNCIONALIDADES_PREMIUM` §4).
2. **AI Add-on:** Copilot + Insights gobernados.
3. **Enterprise-lite:** ledger + períodos + soporte SLA (con UX guiada).
4. **Integraciones conectoras** (pago, lab) — revenue share (`MEDFLOW_FUNCIONALIDADES_QUE_GENERAN_DINERO` §3).
5. **White-label fuerte** — dominio, email, tema (`MEDFLOW_FUNCIONALIDADES_PREMIUM` §2).
6. **API keys por plan** — developer story (`MEDFLOW_ANALISIS_COMPETITIVO`).
7. **Analytics benchmark** — cuando datos y legal permitan.
8. **Marketplace plantillas** — comisión.
9. **Campañas CRM** — solo con marco legal (`MEDFLOW_FUNCIONALIDADES_QUE_GENERAN_DINERO`).
10. **Reportes PDF premium** por vertical.

---

*Priorizar con datos de pilotos: no-show, DSO, horas recepción.*
