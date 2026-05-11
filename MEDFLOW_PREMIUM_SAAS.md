# MedFlow Premium SaaS (Tenant Experience)

**Objetivo:** Que operar MedFlow como **cliente B2B** se sienta como **Stripe / Shopify / HubSpot**: onboarding claro, límites transparentes, facturación justa, API y extensiones.

**Referencias:** `MEDFLOW_FUNCIONALIDADES_PREMIUM.md`, `MEDFLOW_ANALISIS_COMPETITIVO.md`, multitenancy en `ANALISIS_SUPREMO_SISTEMA.md`.

---

## Pilares

| Pilar | Qué debe sentir el tenant |
|-------|---------------------------|
| **Onboarding** | Time-to-value en horas, no semanas |
| **Planes** | Qué está incluido / upgrade obvio |
| **Usage** | Dashboard de cuotas (usuarios, IA, SMS, almacenamiento) |
| **Billing** | Facturas, impuestos, métodos de pago |
| **Developer** | API keys rotativas, webhooks, sandbox |
| **White-label** | Dominio, logo, email desde (enterprise) |

---

## Pricing psychology

- Empaquetar **Growth + IA + Recovery** como escalón superior (no vender “checkboxes”).
- **Trials** con límites duros pero experiencia completa del shell premium.

---

## Product surfaces

1. **Tenant console** (futuro): uso, facturas, seats, integraciones.
2. **Health** del tenant: últimos errores de sync, webhooks fallidos.
3. **Audit** exportable para compliance cliente.

---

## Relación con código actual

- Respetar **aislamiento por tenant** en cada nueva API (tests de fuga obligatorios).
- Feature flags alineados a **plan** en claims o tabla `TenantFeatures`.

---

*Actualizar cuando exista billing metered para IA/SMS.*
