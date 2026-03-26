# Protección de endpoints

## Anónimos intencionados

- `Account/Login`, `Health/*`
- `POST /api/v1/mobile/auth/login`, `POST /api/v1/mobile/auth/refresh`
- `POST /api/v1/auth/staff/login` (nuevo; solo credenciales + tenant)
- `Onboarding/*` (provisionamiento)

## API móvil paciente

- `[Authorize]` + esquema **JwtBearer** en controladores bajo `api/v1/mobile/*` (paciente).

## Riesgo residual

- Endpoints MVC dependen de cookie + `[Authorize]` + `RequirePermission`; no sustituir por confianza en el front.

## Acciones

- Sin `[AllowAnonymous]` indebido en datos sensibles fuera de login/onboarding/health.
