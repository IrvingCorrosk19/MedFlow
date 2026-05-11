# MedFlow — Decisiones ejecutivas (sí / no / cuándo)

**Fuentes obligatorias:** `MEDFLOW_ESTADO_ACTUAL_REAL.md`, `ANALISIS_SUPREMO_SISTEMA.md`, `MEDFLOW_ANALISIS_COMPETITIVO.md`, `MEDFLOW_ROADMAP_PRIORIZADO.md`, `QA_RESULTADOS_COMPLETOS.md`, `ANALISIS_FALTANTES_MODULO_A_MODULO.md`.

---

## 1. Segmento objetivo (decisión de estrategia)

| Decisión | Recomendación basada en evidencia |
|----------|-------------------------------------|
| ¿Competir con Epic/Athena en CIO hospital multinational? | **No** sin programa FHIR + compliance + campo enterprise (`ANALISIS_SUPREMO`, `MEDFLOW_ANALISIS_COMPETITIVO`). |
| ¿Apostar por dueño de clínica / grupo regional / multi-sede LATAM? | **Sí** — ahí encajan ROI rápido, SaaS Stripe, automatización sin SI gigante (`MEDFLOW_ANALISIS_COMPETITIVO` §8). |
| ¿Un vertical antes de dispersarse? | **Sí** — `MEDFLOW_OPORTUNIDADES_BILLONARIAS.md` §3: 1 vertical + 1 región. |

---

## 2. Producto / UX

| Decisión | Acción |
|----------|--------|
| ¿AdminLTE “tal cual” como marca a 3 años? | **No.** Score UX ~5.0 y modernidad ~5.5 (`ANALISIS_SUPREMO`). Design system propietario es prioridad de percepción. |
| ¿Portal paciente dual (`/PatientPortal` + `/portal`) sin decisión? | **No.** Costo soporte + confusión (`MEDFLOW_MODULOS_EXISTENTES`, `MEDFLOW_EXPERIENCIA_CLIENTE`). Elegir canónico y deprecar con plan. |
| ¿Excel/PDF dashboard deshabilitados en demo enterprise? | **Arreglar o ocultar.** Contradicen narrativa premium (`ANALISIS_FALTANTES` §1 histórico vs tabla sync — validar branch). |

---

## 3. Riesgo y ventas

| Decisión | Acción |
|----------|--------|
| ¿Copilot sin hardening en clientes pagadores? | **No.** XSS + rate limit + límites = P0 roadmap (`MEDFLOW_AI_OPPORTUNITIES`, `MEDFLOW_ROADMAP_PRIORIZADO`). |
| ¿CORS permisivo en producción? | **No.** Checklist prod explícito (`ANALISIS_SUPREMO`, `MEDFLOW_ROADMAP_PRIORIZADO`). |
| ¿Prometer FHIR / certificación sin roadmap interno? | **No** en contratos (`MEDFLOW_FUNCIONALIDADES_PREMIUM`, `MEDFLOW_ANALISIS_COMPETITIVO`). |

---

## 4. Ingeniería y operación

| Decisión | Acción |
|----------|--------|
| ¿Seguir sin CI/CD en repo? | **Insostenible** para escala equipo — `MEDFLOW_ESTADO_ACTUAL_REAL` §2.2, `MEDFLOW_ROADMAP_PRIORIZADO` P0. |
| ¿QA solo GET sin POST críticos? | **Insuficiente** para P1 clínico — `QA_RESULTADOS_COMPLETOS`, `PRUEBAS_FLUJOS_PRIORITARIOS_CLINICAS` §8. |

---

## 5. Monetización MedFlow

| Decisión | Acción |
|----------|--------|
| ¿IA incluida ilimitada en todos los planes? | **No** — margen y abuso; modelo Copilot/Insights por tier (`MEDFLOW_AI_OPPORTUNITIES` §3). |
| ¿API móvil sin tiers / keys? | Perder narrativa “Stripe-like”; developer portal + límites (`MEDFLOW_ANALISIS_COMPETITIVO` §4). |

---

## 6. Resumen en una frase

**Apostar por SMB/región con producto unificado clínica–caja–automatización–IA; invertir en confianza (seguridad IA, CI/CD, UX propia) antes de TAM hospitalario o historia “billonaria” sin compliance.**

---

*Revisión trimestral con ventas y legal por país.*
