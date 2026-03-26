# Billing — `qa.billing@medflow.local`

| Ruta | Esperado | HTTP |
|------|----------|------|
| `/` | 200 | 200 |
| `/BillingInvoices` | 200 (facturación) | 200 |
| `/Payments` | 200 | 200 |
| `/Patients` | Sin permiso listado pacientes | 403 |
| `/Appointments` | Sin permiso citas operativas | 403 |
| `/MedicalRecords` | Sin historia clínica | 403 |
| `/AdminUsers` | 403 | 403 |

**Comportamiento:** finanzas/facturación permitidas; aislamiento de clínica operativa y agenda.
