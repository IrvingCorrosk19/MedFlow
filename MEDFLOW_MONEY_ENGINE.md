# MedFlow — Motor de dinero (clínica + MedFlow SaaS)

**Fuentes:** `MEDFLOW_FUNCIONALIDADES_QUE_GENERAN_DINERO.md`, `MEDFLOW_FUNCIONALIDADES_PREMIUM.md`, `MEDFLOW_AI_OPPORTUNITIES.md`, `MEDFLOW_ANALISIS_COMPETITIVO.md`, `PRUEBAS_FLUJOS_PRIORITARIOS_CLINICAS.md`.

---

## Parte A — Dinero para la clínica (money engine operativo)

### Mecanismos ya anclados en producto

| Palanca | Mecanismo | Evidencia |
|---------|-----------|-----------|
| **Agenda + estados** | Utilización de sillones; menos huecos mal gestionados | Dominio `Appointment`, flujos P0 doc pruebas |
| **Historia + recetas** | Episodio facturable coherente | `MedicalRecords`, `Prescriptions`; QA recetas migración |
| **Facturación + pagos + saldo** | Cobro y cartera | `BillingInvoice`, `Payment`; rol Billing QA OK |
| **Caja** | Conciliación diaria | `CashMovement` |
| **Portal paciente** | Confirmación/cancelación con menos fricción telefónica | Portal dual documentado — maximizar cuando UX unificada |
| **Notificaciones / plantillas** | Recordatorios → ↓ no-show | Jobs citados en docs |
| **Analytics ejecutivo** | Precios, dotación, horarios | Dashboard + reporting |

### Pérdidas si falla (explícitas en docs)

- No-show alto, doble agenda, errores facturación, tiempo recepción (`MEDFLOW_FUNCIONALIDADES_QUE_GENERAN_DINERO` §1).

### Funcionalidades que **multiplican** ROI clínica (construir/pulir)

1. **Predicción no-show accionable** + lista de llamadas priorizada — IA (`MEDFLOW_AI_OPPORTUNITIES` §4).
2. **Workflows recordatorio + N8n** — menos FTE recepción (`MEDFLOW_FUNCIONALIDADES_QUE_GENERAN_DINERO` §4).
3. **Calendario visual + anti-conflicto visible** — menos error humano (`ANALISIS_FALTANTES` §4).
4. **Cartera / aging con CTA** — cobrar antes (`MEDFLOW_FUNCIONALIDADES_PREMIUM` dashboard financiero condicionado por rol).
5. **Exports dirección serios** — decisiones fuera del sistema = fuga atención.

---

## Parte B — Dinero para MedFlow (ARR / expansión)

### Ya presente

| Palanca | Mecanismo |
|---------|-----------|
| **Stripe por tenant** | MRR |
| **Planes con límites** | Upsell volumen (`SubscriptionLimitService` citado) |
| **SuperAdmin billing** | Morosidad plataforma |
| **Trials / estados comerciales** | Conversión |

### Monetización incremental (documentada como oportunidad)

| Oferta | Modelo |
|--------|--------|
| **IA Suite** | Por usuario / mensaje / mes / severidad (`MEDFLOW_AI_OPPORTUNITIES` §3) |
| **Integraciones** | Fee por conector o revenue share (`MEDFLOW_FUNCIONALIDADES_QUE_GENERAN_DINERO` §3) |
| **Marketplace plantillas workflow** | Comisión (`MEDFLOW_OPORTUNIDADES_BILLONARIAS` §7) |
| **Telemedicina** | Add-on alto valor si vertical |
| **CRM campañas** | Paquetes compliance-aware |
| **Reports PDF premium** | Por vertical |
| **Benchmark datos** | Opt-in legal (`MEDFLOW_AI_OPPORTUNITIES` §6) |

### Bundling sugerido (alineado `MEDFLOW_FUNCIONALIDADES_PREMIUM` §4)

- **Core:** citas, pacientes, expediente básico, portal básico.
- **Pro:** facturación completa, reportes, notificaciones.
- **Business:** workflows, exports avanzados.
- **Enterprise:** contabilidad, SLA, API keys dedicadas, SSO cuando exista.
- **AI Add-on:** Copilot + Insights gobernados.

---

## Parte C — Métricas que deben existir en pilotos

| Métrica | Por qué |
|---------|---------|
| No-show rate | Demostración ROI IA + notificaciones |
| Días outstanding / aging | ROI módulo financiero |
| Horas semana recepción | ROI workflows + UX citas |
| NPS recepción | Product-market fit operativo |
| Migraciones fallidas deploy | Confianza enterprise (`MEDFLOW_ROADMAP_PRIORIZADO`) |

---

*Este documento es mapa lógico; P&L requiere datos de tenant reales.*
