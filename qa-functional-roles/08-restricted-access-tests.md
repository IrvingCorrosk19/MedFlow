# URL directa — pruebas obligatorias

| Actor | URL atacada | Esperado | Resultado |
|-------|-------------|----------|-----------|
| Patient | `/AdminUsers` | Denegación sin filtrar menú staff | Tras corrección: `/Account/AccessDenied` con layout mínimo y enlace “Volver al portal del paciente” (sin sidebar AdminLTE) |
| Patient | `/Patients`, `/Settings` | 403 | 403 |
| Reception | `/AdminUsers` | 403 | 403 |
| Billing | `/MedicalRecords` | 403 | 403 |
| Doctor | `/AdminUsers` | 403 | 403 |

**Herramienta:** sesión HTTP con `Invoke-WebRequest` + antiforgery en login; navegador en portal paciente + URL directa.

**Hallazgo corregido:** paciente autenticado veía **menú lateral completo del staff** en la página Acceso denegado (`_AdminLayout`). Ver `09-bugs-found-and-fixed.md`.
