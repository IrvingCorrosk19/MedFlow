# MedFlow AI - Deployment & Production Configuration

## Required Environment Variables

Set these in production. Never commit real secrets to source control.

| Variable | Required | Description |
|----------|----------|-------------|
| `ConnectionStrings__DefaultConnection` | Yes | PostgreSQL connection string |
| `Jwt__Secret` | Yes | JWT signing key (min 32 chars) |
| `Stripe__SecretKey` | If billing | Stripe API secret key |
| `Stripe__WebhookSecret` | If billing | Stripe webhook signing secret |
| `Resend__ApiKey` | If email | Resend API key for transactional email |
| `Integrations__N8n__ApiKey` | If workflows | n8n webhook shared token |
| `Cors__AllowedOrigins` | Production | Semicolon-separated origins, e.g. `https://app.example.com` |
| `Cors__AllowAnyOrigin` | Production | Set to `false` |

## Startup Validation

On startup, the application validates:

- Database connection is reachable
- JWT secret is configured
- Critical modules have required configuration (warnings only)

Missing critical config is logged. Set `ASPNETCORE_ENVIRONMENT=Production` for production.

## Health Endpoints

- `GET /health/live` – Liveness (process is running)
- `GET /health/ready` – Readiness (DB, config)
- `GET /health/startup` – Full startup health

Use these for Kubernetes/Docker health probes and load balancers.

## CORS (Production)

Set `Cors__AllowAnyOrigin=false` and provide explicit origins:

```
Cors__AllowedOrigins__0=https://app.medflow.ai
Cors__AllowedOrigins__1=https://admin.medflow.ai
```

Or via JSON in appsettings.Production.json (override with env for real deployments).

## Cookie Security

In production, session cookies use:

- `SecurePolicy=Always` (HTTPS only)
- `SameSite=Lax` (configurable via `Security__CookieSameSite`)

## Swagger

If Swagger/OpenAPI is added, restrict it to Development only.

## Ops Dashboard

SuperAdmin users can access `/Ops` for:

- Health status
- Worker heartbeats
- Stripe webhook event log (failed/recent)
