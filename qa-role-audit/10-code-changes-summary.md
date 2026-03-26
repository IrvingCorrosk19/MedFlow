# Cambios de código

| Archivo | Cambio |
|---------|--------|
| `src/MedFlow.Application/Interfaces/ITenantStaffAuthService.cs` | Nuevo — contratos staff JWT |
| `src/MedFlow.Infrastructure/Identity/TenantStaffAuthService.cs` | Nuevo — login staff + refresh |
| `src/MedFlow.Infrastructure/DependencyInjection.cs` | Registro `ITenantStaffAuthService` |
| `src/MedFlow.Web/Controllers/Api/TenantStaffAuthController.cs` | Nuevo — `POST api/v1/auth/staff/login` |
| `src/MedFlow.Infrastructure/Persistence/DataSeeder.cs` | `TenantId` en update; `SeedQaPatientPortalPatientAsync` |
| `src/MedFlow.Web/appsettings.Development.json` | `Saas:AllowOperationsWhenPastDue` |
