# MedFlow — Experiencia portal paciente

## Estado

- Dos superficies históricas: área `PatientPortal` y rutas `/portal/*` (`PatientPortalController`).
- **Middleware canónico** redirige GET legacy → `/portal/...` según mapa exacto.

## Cambio implementado

- **`_PatientLayout.cshtml`**: banner informativo dismissible con enlace a **`/portal/dashboard`** como experiencia recomendada (especialmente móvil).

## Próximo paso producto (grande)

1. Elegir **una** superficie principal para nuevas features (recomendación: `/portal`).
2. Migrar vistas restantes del área a controladores unificados o mantener redirects permanentemente.
3. **AI patient assistant**: solo tras política de datos y límites legales por jurisdicción.

---

*Notificaciones realtime: requiere SignalR + estrategia push ya parcialmente en API móvil.*
