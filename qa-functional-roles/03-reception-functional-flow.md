# Reception — `qa.reception@medflow.local`

| Ruta | Esperado | HTTP |
|------|----------|------|
| `/` | 200 | 200 |
| `/Appointments` | 200 (citas) | 200 |
| `/Patients` | 200 | 200 |
| `/AdminUsers` | 403 | 403 |
| `/Settings` | 403 | 403 |
| `/NotificationTemplates` | 403 | 403 |
| `/MedicalRecords` | Sin acceso clínico | 403 |

**Comportamiento:** recepción opera citas y pacientes; no administración ni historia clínica completa.
