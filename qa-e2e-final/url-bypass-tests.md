# URL Bypass / Authorization Tests

All tests verify that direct URL navigation to unauthorized resources results in an Access Denied response. No role can escalate privileges via URL manipulation.

---

## Test Matrix

| Role | Attempted URL | Expected | Result |
|------|--------------|----------|--------|
| **Doctor** | `/BillingInvoices` | Access Denied | ✅ |
| **Doctor** | `/CashMovements` | Access Denied | ✅ |
| **Doctor** | `/Admin/Users` | Access Denied | ✅ |
| **Doctor** | `/Admin/Roles` | Access Denied | ✅ |
| **Reception** | `/MedicalRecords` | Access Denied | ✅ |
| **Reception** | `/BillingInvoices` | Access Denied | ✅ |
| **Reception** | `/Admin/Users` | Access Denied | ✅ |
| **Billing** | `/MedicalRecords` | Access Denied | ✅ |
| **Billing** | `/Appointments` | Access Denied | ✅ |
| **Billing** | `/Admin/Users` | Access Denied | ✅ |
| **Staff** | `/MedicalRecords` | Access Denied | ✅ |
| **Staff** | `/BillingInvoices` | Access Denied | ✅ |
| **Staff** | `/Admin/Users` | Access Denied | ✅ |
| **Staff** | `/CashMovements` | Access Denied | ✅ |
| **Patient** | `/Appointments` | Access Denied (staff route) | ✅ |
| **Patient** | `/BillingInvoices` | Access Denied | ✅ |
| **Patient** | `/MedicalRecords` | Access Denied | ✅ |
| **Patient** | `/Admin/Users` | Access Denied | ✅ |
| **Patient** | `/Dashboard` | Access Denied | ✅ |

---

## Tests Executing These Checks

| Test | Roles Covered |
|------|--------------|
| `Doctor_CannotAccess_BillingCashAdminSecurity_Routes` | Doctor |
| `Reception_CannotAccess_ClinicalBillingAdmin_Routes` | Reception |
| `Billing_CannotAccess_ClinicalAppointmentsAdmin_Routes` | Billing |
| `Staff_CannotAccess_ClinicalBillingAdmin_Routes` | Staff |
| `Staff_CanViewPatients_And_IsRestricted_FromClinicalAdminBilling` | Staff (with allowed routes) |
| `Patient_Portal_CannotAccess_AnyStaffRoute` | Patient |
| `Patient_Portal_CanViewProfile_And_BypassAttempts_AreBlocked` | Patient |
| `Staff_CanCreateEditSaveCancelAppointment_And_IsRestrictedFromClinicalBilling` | Staff (in-flow) |

---

## Authorization Mechanism

The application uses `[RequirePermission(PermissionCode)]` attribute (`IAsyncAuthorizationFilter`) on controllers and actions. If the logged-in user lacks the required permission:

- Returns `ForbidResult()` → ASP.NET Core redirects to `/Account/AccessDenied`
- The Access Denied page shows role-appropriate navigation (Patient Portal link vs. Admin Dashboard link)
- Unauthenticated access results in `ChallengeResult()` → login redirect

---

## Staff Special Case

Staff can view the Patients list (`/Patients`) but cannot:
- Access `/MedicalRecords`
- Access `/BillingInvoices`
- Access `/Admin/*`
- Access `/CashMovements`

This nuance is covered in `Staff_CanViewPatients_And_IsRestricted_FromClinicalAdminBilling`.
