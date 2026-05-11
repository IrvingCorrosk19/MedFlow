# MedFlow — Funcionalidades que generan dinero (clínica + MedFlow como SaaS)

**Marco:** Dinero = **ingresos capturados**, **menos fugas**, **menos costo operativo**, **más retención**, **ARR SaaS MedFlow**.

---

## 1. Para la **clínica** (valor económico directo)

| Capacidad en producto | Mecanismo de dinero | Evidencia |
|------------------------|---------------------|-----------|
| **Agenda + estados de cita** | Reduce huecos mal gestionados; permite priorizar y cobrar servicios | Entidad `Appointment`, estados en dominio |
| **Historia clínica + recetas** | Habilidad de facturar episodio coherente; menos pérdida de cargo por papel | `MedicalRecords`, `Prescriptions` |
| **Facturación + pagos + saldo** | Cobro y control de cartera | `BillingInvoice`, `Payment`, QA Billing |
| **Caja** | Conciliación operativa diaria | `CashMovement` |
| **Portal paciente** | Confirmaciones/cancelaciones con menos fricción telefónica; preparación de cita (mejoras recientes orientadas a reducción no-show) | Áreas portal + doc QA TP-H |
| **Notificaciones / plantillas** | Recordatorios → menos no-show → más utilización de agenda | `NotificationTemplate`, jobs citados en docs |
| **Analytics ejecutivo** | Decisiones de precios, dotación, horarios | Dashboard + servicios analytics |

**Qué hace **perder** dinero si falla (del plan de pruebas + negocio): no-show alto, doble agenda, errores de facturación, fugas de tiempo recepción.

---

## 2. Para **MedFlow** como vendedor SaaS (ARR/MRR)

| Capacidad | Mecanismo |
|-----------|-----------|
| **Suscripción Stripe por tenant** | MRR predecible |
| **Planes con límites** (`SubscriptionLimitService`, etc.) | Upsell por volumen de usuarios/pacientes/citas |
| **Trials / estados comerciales** | Conversión y expansión |
| **SuperAdmin billing** | Control morosidad, facturas plataforma |
| **Add-ons futuros** | IA, integraciones premium, white-label |

---

## 3. Funcionalidades que pueden generar **más** dinero ( roadmap monetización)

| Oportunidad | Por qué |
|-------------|---------|
| **API / integraciones de pago y laboratorio** | Revenue share o tarifa por conector |
| **IA gobernada** | Tier “AI Suite” con límites por mensajes/insights |
| **Marketplace plantillas workflow** | Comisión por plantilla certificada |
| **Telemedicina** | Tarifa por minuto/sesión o add-on alto valor |
| **Campañas CRM pacientes** | Paquetes marketing HIPAA-aware (según jurisdicción) |
| **Reports PDF premium** | Vendibles por vertical |

---

## 4. Automatización = dinero (menos FTE)

| Área | Ejemplo |
|------|---------|
| **Workflows + N8n** | Menos trabajo manual recepción/facturación |
| **Insights IA** | Priorizar cohortes de riesgo (no-show, cartera) |
| **Exportaciones programadas** | Menos Excel manual dirección |

---

## 5. Conclusión

MedFlow ya conecta **operación clínica** con **cobro** y tiene **motor SaaS**. El salto de ingresos viene de: **empaquetar** IA/workflows/analytics como **add-ons medibles**, **reducir fricción del portal**, y **demostrar ROI** (no-show, días en AR, horas admin).

---

*Validar métricas reales con datos de pilotos; este documento es mapa lógico, no P&L.*
