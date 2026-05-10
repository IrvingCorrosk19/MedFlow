# MedFlow — seguridad enterprise (programa hardening)

**Versión:** 1.0 · **Fecha:** 2026-05-10  
**Referencias:** `ANALISIS_SUPREMO_SISTEMA.md` (seguridad ~6.5/10), `QA_RESULTADOS_COMPLETOS.md`, `MEDFLOW_GAPS_RESTANTES.md`.

---

## Postura objetivo

MedFlow maneja datos de salud y financieros: la barra no es “sin errores obvios”, sino **defensa en profundidad** alineada con OWASP ASVS (nivel objetivo por contrato cliente) y **aislamiento tenant** demostrable.

---

## Controles ya observados en código (supremo + práctica)

- ASP.NET Core Identity, JWT opcional, middleware headers, rate limiting (configurable), correlación, health checks, OpenTelemetry hooks.
- Tests de aislamiento tenant en suite unitaria (buena base).

---

## Programa OWASP-oriented (priorizado)

### A01 — Broken Access Control

| Control | Acción |
|---------|--------|
| Autorización por acción | `[Authorize]` + `[RequirePermission]` en **todos** los métodos; revisión API Mobile V1 y webhooks |
| Ownership | Validar que `PatientId`/`DoctorId` pertenece al tenant y rol antes de mutación |
| SuperAdmin | Rutas `SuperAdmin/*` con barrera extra y auditoría |

### A02 — Cryptographic Failures

- TLS en prod; secrets solo en Key Vault / env seguros; no semillas QA en prod.

### A03 — Injection

- EF parametrizado por defecto; revisar SQL raw si existe.
- HTML en vistas: evitar `@Html.Raw` no confiable en contenido usuario.

### A04 — Insecure Design

- Threat modeling trimestral por feature PHI/finanzas.

### A05 — Security Misconfiguration

- CORS restrictivo por origen en prod (**supremo:** permisivo si no se define).
- Desactivar detalles error stack a usuarios finales.

### A07 — Identification & Authentication

- 2FA donde política tenant; lockout y logs de intentos.

### A08 — Software/Data Integrity

- Dependabot / actualización paquetes; firma de webhooks (Stripe).

### A09 — Logging & Monitoring Failures

- Sin PII en logs estructurados; correlación request-id; alertas intentos anómalos.

### A10 — SSRF

- Callbacks salientes solo URLs allowlist (N8n, integraciones).

---

## Uploads (crítico expediente/adjuntos)

- Validación extensión **y** MIME sniff conservador; tamaño máximo por tipo; nombres sanitizados; almacenamiento fuera de webroot o con controlled URLs.

---

## Tenant isolation

- Convención: todo query dominio pasa por filtros tenant salvo `IgnoreTenantFilter` explícito y auditado.
- Tests de regresión por cada nuevo `DbSet` expuesto.

---

## Audit trail

- Eventos administrativos y clínicos sensibles en `AuditLog` / `EventLog` con actor, tenant, entidad.
- Retención acorde a política cliente.

---

## Rate limiting

- Producción: activo; límites diferenciados login vs API vs webhooks.

---

## Roadmap certificación (opcional por mercado)

- SOC 2 Type II-style narrative requiere políticas **documentadas**, no solo código — coordinar con legal/compliance.

---

## Checklist release (extracto)

Ver `MEDFLOW_WORLDCLASS_CHECKLIST.md` sección seguridad.

---

*Pentest externo obligatorio antes de claims públicos “bank-level” o equivalente.*
