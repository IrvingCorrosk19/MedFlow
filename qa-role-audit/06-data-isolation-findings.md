# Aislamiento de datos

- JWT incluye `tenant_id`; servicios usan `ITenantContext` + filtros EF en entidades con tenant.
- Staff login exige `tenantCode` y `user.TenantId` coincide con tenant resuelto.
- Patient: acceso API resuelve `patientId` por `UserId` — sin vínculo, login móvil falla (comportamiento seguro).

## Pruebas de ataque (muestra)

- Sin token en API paciente → 401.
- Staff password incorrecta → 401.
- Patient en endpoint staff → 401.

## Pendiente manual

- Segundo tenant en BD + prueba de cruce de `tenant_id` en API (no automatizado en esta pasada).
