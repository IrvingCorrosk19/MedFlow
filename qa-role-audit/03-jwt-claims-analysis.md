# JWT — claims

## Emisión

- **Staff (Admin–Staff):** `POST /api/v1/auth/staff/login` con `email`, `password`, `tenantCode` (obligatorio).
- **Patient:** `POST /api/v1/mobile/auth/login` (solo rol Patient + vínculo `Patients.UserId` + portal habilitado).

## Claims observados (access token)

| Claim | Valor típico |
|-------|----------------|
| `iss` | `MedFlow` (`Jwt:Issuer`) |
| `aud` | `MedFlow.Mobile` (`Jwt:Audience`) |
| `http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier` | UserId |
| `http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress` | Email |
| `tenant_id` | GUID tenant (demo) |
| `http://schemas.microsoft.com/ws/2008/06/identity/claims/role` | Rol (uno por token en usuarios QA) |
| `exp` | UTC |

## Expiración

- `AccessTokenExpirationMinutes` (appsettings, p. ej. 15).
- Refresh: `RefreshTokenExpirationDays` (p. ej. 7).

## Negativos

- Contraseña incorrecta staff login → **401**.
- Patient en staff login → **401** (debe usar login móvil paciente).
- Dashboard paciente sin `Authorization` → **401**.
