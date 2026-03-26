# Admin — `qa.admin@medflow.local`

| Pantalla / acción | Esperado | Real (HTTP GET) | Real (UI) |
|-------------------|----------|-----------------|-----------|
| `/` Dashboard | 200 | 200 | Menú completo (Clínica, Reportes, Finanzas, Seguridad, Configuración…) |
| `/AdminUsers` | 200 | 200 | Tabla usuarios, búsqueda, paginación, Nuevo usuario |
| `/Patients` | 200 | 200 | |
| `/Settings` | 200 | 200 | |
| `/NotificationTemplates` | 200 | 200 | |
| `/Commercial/Blocked` | Accesible (página comercial) | 200 | |

**Acciones:** login formulario válido; listado AdminUsers con controles DataTables (combobox registros, buscar, paginación).

**Errores:** ninguno en flujo probado.
