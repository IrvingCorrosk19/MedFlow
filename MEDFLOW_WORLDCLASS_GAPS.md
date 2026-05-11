# MedFlow — Brechas “world-class” (honesto + accionable)

**Escala de referencia:** Linear / Notion / Stripe (DX + pulimiento), no Epic (interoperabilidad masiva).

**Fuentes:** `ANALISIS_SUPREMO_SISTEMA.md`, `MEDFLOW_EXPERIENCIA_CLIENTE.md`, `MEDFLOW_QUE_FALTA_PARA_SER_EL_MEJOR.md`, `ANALISIS_FALTANTES_MODULO_A_MODULO.md`, `QA_RESULTADOS_COMPLETOS.md`.

---

## 1. Brecha visual / marca

| Gap | Evidencia | Impacto |
|-----|-----------|---------|
| AdminLTE + Bootstrap 4 como columna visual | Score modernidad ~5.5, diseño ~5.0 (`ANALISIS_SUPREMO`) | Comprador asocia “software interno 2018”. |
| Sin design system propietario reconocible | `MEDFLOW_EXPERIENCIA_CLIENTE` §2 | Imposible sensación “tier-1” sin rediseño frontal. |
| Tablas/DataTables dominantes | Supremo §6–7 | Eficientes; no generan amor de usuario. |

**Qué cerraría el gap más rápido:** tokens + tipografía + shell + empty states + menos densidad en vistas hero (`MEDFLOW_EXPERIENCIA_CLIENTE` §7).

---

## 2. Brecha producto “Stripe-like”

| Gap | Evidencia |
|-----|-----------|
| Onboarding tenant sin equivalencia “Checkout-level” | `ANALISIS_SUPREMO` vs Stripe comparativa |
| Sin developer portal / OpenAPI clase mundial | `MEDFLOW_ANALISIS_COMPETITIVO` §4 |
| Integraciones como historia débil vs Shopify ecosystem | Misma sección |

**Acción:** API keys por tier + documentación generada + sandbox webhook story.

---

## 3. Brecha tiempo real / colaboración

| Gap | Evidencia |
|-----|-----------|
| No hay equivalente Cmd-K omnicanal | Supremo vs Notion/Linear |
| Sin colaboración tiempo real | `MEDFLOW_ANALISIS_COMPETITIVO` §5 |

**Acción:** command palette + “mi día” como primer paso sin reescribir todo a SPA.

---

## 4. Brecha datos / storytelling ejecutivo

| Gap | Evidencia |
|-----|-----------|
| Dashboard sin drill-down accionable consistente | `MEDFLOW_QUE_FALTA_PARA_SER_EL_MEJOR`, `ANALISIS_FALTANTES` §1 |
| Excel/PDF aún deuda según secciones históricas | `ANALISIS_FALTANTES` §1 (contrastar tabla sync) |
| IA no integrada visualmente al Mission Control | `ANALISIS_FALTANTES` §1 integración |

**Acción:** cada KPI → lista filtrada en un clic; Insights embebidos con CTA.

---

## 5. Brecha confianza enterprise

| Gap | Evidencia |
|-----|-----------|
| Sin programa formal SOC2/HIPAA/GDPR como producto | `ANALISIS_SUPREMO`, `MEDFLOW_QUE_FALTA_PARA_SER_EL_MEJOR` §3 |
| Sin pentest recurrente documentado | Misma fuente |
| GET sin permiso granular en módulos (deuda histórica) | `ANALISIS_FALTANTES` Pacientes/Citas/Expediente |

**Acción:** auditoría de rutas + política de releases; compliance narrative solo cuando ICP lo exija.

---

## 6. Brecha interoperabilidad

| Gap | Evidencia |
|-----|-----------|
| Sin FHIR como producto | `MEDFLOW_ANALISIS_COMPETITIVO`, `ANALISIS_SUPREMO` |
| Integraciones vía webhooks ≠ red clínica enterprise | `MEDFLOW_OPORTUNIDADES_BILLONARIAS` §4 |

**Acción:** FHIR read-first en vertical elegido o hub de partners — no ambos a la vez sin capital.

---

## 7. Brecha operativa (no código)

| Gap | Evidencia |
|-----|-----------|
| CI/CD ausente en repo | `MEDFLOW_ESTADO_ACTUAL_REAL` §2.2 |
| SLIs/SLOs no institucionalizados | `MEDFLOW_QUE_FALTA_PARA_SER_EL_MEJOR` §2 |

---

## 8. Triple coincidencia “supremo”

World-class en esta categoría de producto = simultáneamente:

1. **Belleza y velocidad percibida**
2. **Fiabilidad demostrable** (uptime, seguridad, soporte)
3. **ROI medible** (no-show, cartera, horas admin)

(`ANALISIS_SUPREMO` §14)

---

## 9. Qué NO es brecha crítica para el ICP SMB

- Paridad feature con Epic — descartado sin TAM y equipo (`MEDFLOW_ANALISIS_COMPETITIVO`).
- Microservicios — monolito modular es ventaja operativa actual (`MEDFLOW_ESTADO_ACTUAL_REAL`).

---

*Actualizar tras cerrar diseño system y primer pipeline CI.*
