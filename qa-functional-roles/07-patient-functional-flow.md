# Patient — `qa.patient@medflow.local`

**Login:** `/PatientPortal/login` (no `/Account/Login`).

| Ruta | Esperado | HTTP |
|------|----------|------|
| `/PatientPortal/inicio` | 200, dashboard paciente | 200 |
| `/PatientPortal/perfil` | 200 | 200 |
| `/Patients` (staff) | Bloqueado | 403 |
| `/Settings` (staff) | Bloqueado | 403 |
| `/AdminUsers` | Bloqueado; sin chrome staff | 403 + vista mínima (tras fix) |

**UI:** “Hola, QA Paciente”; menú portal (Mi Perfil, Mis Citas, Facturas, Pagos, Cerrar sesión).

**Rutas portal:** citas en `/PatientPortal/citas`, historial `/PatientPortal/citas/historial` (no usar `/PatientPortal/Appointments/...` — 404).
