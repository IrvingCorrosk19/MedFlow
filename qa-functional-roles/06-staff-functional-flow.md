# Staff — `qa.staff@medflow.local`

| Ruta | Esperado | HTTP |
|------|----------|------|
| `/` | 200 | 200 |
| `/Patients` | 200 | 200 |
| `/AdminUsers` | 403 | 403 |
| `/Settings` | 403 | 403 |
| `/NotificationTemplates` | 403 | 403 |

**Comportamiento:** operación básica; sin administración ni plantillas de notificaciones.
