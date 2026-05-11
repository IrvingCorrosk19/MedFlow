# MedFlow — IA: implementación y límites

## Ya existente (producto)

- **Growth AI** strip en dashboard con reglas sobre KPIs (`_GrowthAiInsights.cshtml`).
- **Copilot** operativo (`CopilotController.Query`) con sanitización UX vía `.text()` en cliente (anti-XSS en render).
- Áreas **AI**: Insights, AIDashboard, Growth Engine (según permisos).

## Implementado ahora

| Ítem | Detalle |
|------|---------|
| **Rate limiting Copilot** | Con rate limiting global habilitado: máx. **24** peticiones/minuto por usuario (o IP si no autenticado) en `POST` que contiene `/Copilot/Query`. |
| **UI Copilot** | Experiencia “premium glass”; maxlength 500 en HTML alineado al servidor. |

## No implementado (requiere trabajo IA/datos)

- Modelos de predicción no-show entrenados (dataset histórico etiquetado).
- Scheduling optimization con restricciones de sala/equipo.
- Smart notifications push sin proveedor contratado.

## Configuración

- Desarrollo suele tener `RateLimiting:Enabled: false` — activar en staging/prod para efecto real.

---

*Política de datos sensibles: revisar con legal antes de prompts clínicos almacenados.*
