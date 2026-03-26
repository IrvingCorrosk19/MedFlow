# Doctor — `qa.doctor@medflow.local`

| Ruta | Esperado | HTTP |
|------|----------|------|
| `/` | 200 | 200 |
| `/Patients` | 200 | 200 |
| `/MedicalRecords` | 200 | 200 |
| `/AdminUsers` | 403 | 403 |
| `/Settings` | 403 | 403 |

**Comportamiento:** acceso clínico (`/MedicalRecords`); bloqueo de módulos administrativos.
