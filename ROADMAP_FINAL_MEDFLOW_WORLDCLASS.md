# Roadmap final — MedFlow hacia plataforma SaaS enterprise world-class

**Versión:** 1.0 · **Fecha:** 2026-05-10  
**Fuentes obligatorias integradas:** `ANALISIS_FALTANTES_MODULO_A_MODULO.md`, `ANALISIS_SUPREMO_SISTEMA.md`, `QA_RESULTADOS_COMPLETOS.md`, `PRUEBAS_FLUJOS_PRIORITARIOS_CLINICAS.md`

---

## Visión north-star

MedFlow debe percibirse como **software premium contemporáneo** (referencia: Stripe, Linear, HubSpot en claridad y velocidad percibida; Salesforce Health / Athena en ambición clínica comercial), sin sacrificar **seguridad multi-tenant**, **trazabilidad** y **operabilidad clínica diaria**.

**Promesa medible (24–36 meses):** time-to-value en onboarding &lt; 15 min; latencia percibida en vistas P0 &lt; 200 ms con SLIs publicados; zero critical en auditoría OWASP interna por release.

---

## Principios de ejecución

1. **Confianza antes que brillo:** cerrar huecos de permisos, validación, fugas de tenant y errores 500 en flujos P0/P1 antes de expansión visual masiva.
2. **Un solo design system:** tokens, tipografía, componentes; eliminar excepciones “por vista”.
3. **IA con gobernanza:** cada capacidad IA con trazabilidad, límites de datos y evaluación offline cuando afecte decisiones sensibles.
4. **Medición continua:** dashboards de producto (activación, retención, tiempo en flujo clínico) + observabilidad técnica (OTel, SLOs).

---

## Fase 0 — Línea base (completada / en curso según repo)

| Área | Estado (según docs 2026-05-10) |
|------|--------------------------------|
| Dashboard ejecutivo | Días 7–90, try/catch, CSV, KPIs con enlaces, bloque financiero con `billing.view`, vista null-safe |
| Pacientes | Permisos, filtros, búsqueda ampliada, CSV, unicidad documento, delete con reglas, SetActive, empty state |
| QA | Roles verificados en muestra; DataTables ES local; migración prescripciones |
| Deuda documentada | Texto histórico en `ANALISIS_FALTANTES_*` — muchos ítems **obsoletos**; usar `MEDFLOW_GAPS_RESTANTES.md` |

---

## Onda 1 (0–3 meses) — Confiabilidad + “no más vergüenzas”

**Objetivo:** ningún flujo P0/P1 falla silenciosamente; permisos y validaciones consistentes.

- Auditoría **sistemática** de `[Authorize]` + `[RequirePermission]` en **todos** los GET/POST de controllers y áreas (`SuperAdmin`, `AI`, `PatientPortal`, APIs).
- Matriz de pruebas **POST** (crear paciente/cita, pagos) según `PRUEBAS_FLUJOS_PRIORITARIOS_CLINICAS.md` §8.
- Uploads: MIME/extensión/tamaño; paths seguros; virus scan en roadmap si el mercado lo exige.
- Dashboard: caché de lectura (Redis/memory) con TTL por tenant para query pesada; skeleton loader en UI.
- **CI/CD:** pipeline mínimo (build + test + migraciones dry-run en staging).

**Salida:** versión “staging-ready” con checklist verde en `MEDFLOW_WORLDCLASS_CHECKLIST.md` (bloque P0/P1).

---

## Onda 2 (3–9 meses) — Premium percibido (UI/UX)

**Objetivo:** dejar de verse “AdminLTE genérico” sin reescribir todo el backend.

- Design system único: consolidar `medflow-theme.css` + `medflow-premium.css`; documentar tokens en `MEDFLOW_UI_UX_REDESIGN.md`.
- Shell: sidebar/colapsado, densidad configurable, dark mode real (preferencia usuario + sistema).
- Tablas: menos como única vista — vistas resumen + drill-down; paginación server-side en listados críticos.
- Empty states, errores y loading **siempre** con componentes compartidos.
- Portal paciente: layout “app-like”, timeline clínica, PDFs facturas/recetas.

**Salida:** benchmark UX interno (SUS / task completion) + reducción de clicks en flujos P0.

---

## Onda 3 (6–18 meses) — IA útil (no decorativa)

Ver `MEDFLOW_AI_ROADMAP.md`.

- Copilot contextual por módulo con tool-calling sobre APIs internas auditadas.
- Resúmenes paciente/cita con citaciones a registros.
- Alertas operativas (no-show, cartera) con explicación y acción sugerida.

**Salida:** políticas de retención de prompts, evaluación por dominio clínico, human-in-the-loop donde aplique normativa.

---

## Onda 4 (9–24 meses) — Enterprise + escala

- API keys graduadas, rate limits por clave, contratos versionados.
- Read replicas / particionamiento selectivo según métricas.
- Paquete compliance **vendible** (proceso SOC2-style o equivalente mercado objetivo).
- FHIR **roadmap por fases** (read-first, luego write selectivo).

---

## Onda 5 (18–36 meses) — Ecosistema

- Marketplace de integraciones gobernado.
- Vertical dominante (ej. multi-sede LATAM) antes de dispersión geográfica.

---

## Métricas de éxito (KPIs programa)

| KPI | Meta |
|-----|------|
| Uptime / error rate | SLO acordado por entorno |
| Tiempo medio flujo “nueva cita” | ↓ 30% vs baseline |
| % vistas con empty state diseñado | 100% P0/P1 |
| Cobertura tests automatizados críticos | ↑ continua |
| Hallazgos críticos seguridad interna | 0 en release |

---

## Dependencias críticas

- **Legal/regulatorio:** depende del mercado (salud); el roadmap técnico no sustituye asesoría legal.
- **Datos de producción:** sin métricas reales, los SLIs son aspiracionales — instrumentar OTLP en staging/prod.

---

*Este roadmap es contrato de dirección de producto e ingeniería; la ejecución prioriza ondas en paralelo solo cuando los equipos y el riesgo lo permiten.*
