# MedFlow Revenue Automation

**Propósito:** Convertir **olvido operativo** en **dinero recuperado** — pagos pendientes, pacientes inactivos, citas rotas, seguimientos — con **cumplimiento** y **opt-in**.

**Contexto:** `MEDFLOW_FUNCIONALIDADES_PREMIUM.md`, gaps en `ANALISIS_FALTANTES_MODULO_A_MODULO.md`, QA en `PRUEBAS_FLUJOS_PRIORITARIOS_CLINICAS.md`.

---

## Casos de uso (prioridad)

| Prioridad | Caso | Canal preferido | KPI |
|-----------|------|-----------------|-----|
| P0 | Factura próxima a vencer / vencida | Email + SMS | DSO ↓ |
| P1 | Paciente sin control en X meses | WhatsApp (donde legal) | Visitas recurrentes ↑ |
| P2 | Cita cancelada → re-booking | SMS deep link portal | Ocupación ↑ |
| P3 | Abandono tras primera visita | Secuencia multi-touch | LTV ↑ |

---

## Arquitectura lógica

1. **Triggers:** eventos de dominio (factura creada, cita cancelada, última visita > N días).
2. **Reglas por tenant:** plantillas, horarios silenciosos, idioma, límites diarios.
3. **Cola + proveedor:** SMS/WhatsApp (Twilio/Meta), email (existente), fallbacks.
4. **Atribución:** cada mensaje lleva `campaignId` / `workflowRunId` para reporting en Growth Engine.

---

## Plantillas (ejemplos de copy)

- *“Hola {nombre}, hace {meses} meses sin control. Agenda en 1 toque: {link}”*
- *“Tu factura vence mañana — paga aquí: {link}”*
- *“Quedó un hueco mañana {hora} con {doctor}. Reserva: {link}”*

**Importante:** disclaimers médicos y normativa local (HAROPE, consentimiento marketing).

---

## Integración con IA

- **No** generar diagnósticos; **sí** optimizar horario de envío, tono, y segmentación (A/B).
- **Human approval** opcional para primer envío masivo por campaña.

---

## Seguridad

- Links firmados y de corta duración.
- Rate limit por paciente y por tenant.
- Auditoría: quién aprobó qué campaña.

---

## Roadmap incremental

1. **Workflows** sobre eventos ya emitidos (facturas, citas).
2. Dashboard **“Recuperado este mes”** en Mission Control.
3. Conector **n8n** para clínicas enterprise que quieren orquestar fuera.

---

*Actualizar con métricas reales cuando el primer workflow esté en producción.*
