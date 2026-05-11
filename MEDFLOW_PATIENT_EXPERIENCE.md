# MedFlow Patient Experience Platform

**Visión:** Un solo **Patient Experience Platform** — timeline médica elegante, pagos, resultados, recetas, notificaciones — sensación **Apple Health / Stripe Customer Portal**.

**Contexto obligatorio:** `MEDFLOW_EXPERIENCIA_CLIENTE.md`, rutas `/portal` vs `/PatientPortal`, gaps en `ANALISIS_FALTANTES_MODULO_A_MODULO.md`, QA `QA_RESULTADOS_COMPLETOS.md`.

---

## Estado de unificación

| Objetivo | Acción |
|----------|--------|
| **Una marca, una URL canónica** | Redirect 301 de legado → canónico; un solo layout “premium” |
| **Navegación coherente** | Mismo header, mismos tokens `--mf-*` |
| **Feature parity** | Inventario de pantallas en ambas rutas; migración por sprint |

**Trabajo reciente (handoff):** parciales de guidance de citas, opciones `PatientPortalOptions`, vistas alineadas — continuar hasta **cero duplicación** de flujos críticos.

---

## Pilares UX paciente

1. **Timeline** — eventos clínicos y administrativos en una sola línea temporal.
2. **Claridad** — próxima acción obvia (pagar, reagendar, subir documento).
3. **Confianza** — estados de carga skeleton, errores humanos, sin jerga ERP.
4. **Mobile-first** — touch targets, bottom nav opcional en PWA fase 10.

---

## IA paciente (segura)

- **Asistente no clínico:** orientación sobre **cómo usar el portal**, citas, pagos.
- **No** sustituir consejo médico; enlaces a profesional ante síntomas.

---

## Pagos y facturación

- Checkout con **resumen claro**, métodos guardados según PCI vía proveedor.
- Recordatorios inteligentes enlazados a **Revenue Automation**.

---

## Métricas

- Tasa de **self-service** (reagenda sin llamada).
- **Time-to-pay** desde notificación.
- **NPS** post-visita in-app.

---

*Owner producto: revisar tras cada release de portal.*
