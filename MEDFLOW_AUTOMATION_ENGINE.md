# MedFlow — Motor de automatización

## Estado actual

- **Workflow definitions / executions** en dominio; UI Automations + WorkflowExecutions.
- **Triggers** documentados en `RevenueRecoveryController` (invoice overdue, patient inactive, no-show risk, etc.).
- **Webhooks** hacia n8n para acciones externas.

## Cambios relacionados

- Rate limit Copilot **no** altera workflows; protege costo LLM y abuso API staff.

## Próximos pasos de producto

1. Plantillas JSON por vertical ya en `wwwroot/workflow-templates/` — empaquetar “starter packs” en onboarding.
2. Panel “últimas ejecuciones recovery” enlazado desde Mission Control (ya hay enlaces desde Revenue Recovery).

---

*Automatización útil = eventos reales del dominio clínico-financiero, no triggers decorativos.*
