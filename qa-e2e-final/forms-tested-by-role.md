# Forms Tested by Role

## Admin

| Form | Positive Case | Negative Case(s) |
|------|--------------|-----------------|
| **Create User** | Fill all fields → success flash | Empty email, weak password, duplicate email |
| **Edit User** | Change role → success flash | No negative (edit is pre-validated) |
| **Create Patient** | Fill required fields → success | Empty name/DOB → validation message |
| **Edit Patient** | Update name → saved and persisted | — |
| **Create Doctor** | Fill specialty/license → success | Empty required fields → validation |
| **Edit Doctor** | Update specialty → saved | — |
| **Admin Role Management** | Assign permission checkboxes | Custom checkbox intercept (Bootstrap custom-control) |

**Tests covering Admin:** `Admin_CanCreateEditSaveCancelUser_WithValidation`, `Admin_CanCreateEditSaveCancel_Patient_WithValidation`, `Admin_CanCreateEditSaveCancel_Doctor_WithValidation`, `Admin_CannotCreateUser_WithDuplicateEmail_OrWeakPassword`

---

## Reception

| Form | Positive Case | Negative Case(s) |
|------|--------------|-----------------|
| **Create Appointment** | Fill patient/doctor/date/time → success flash | Missing fields → stays on Create; schedule conflict → rejected |
| **Edit Appointment** | Update consultation room → saved | Empty DoctorId → validation visible |
| **Cancel Appointment** | Click Cancel link → redirect | — |
| **Create Patient** | Fill required fields → success | Empty name → validation message |
| **Edit Patient** | Update phone → saved | — |

**Tests covering Reception:** `Reception_CanCreateEditSaveCancelAppointment_WithValidation`, `Reception_CanCreateEditSaveCancel_Patient_WithValidation`, `Reception_CannotCreateAppointment_WithoutPatient`

---

## Doctor

| Form | Positive Case | Negative Case(s) |
|------|--------------|-----------------|
| **Create Medical Record** | Fill diagnosis + prescription → success | Empty DoctorId → ModelState error visible |
| **Edit Medical Record** | Update diagnosis → saved, redirect to Details | Empty DoctorId → stays on Edit |
| **Cancel Edit** | Cancel link → back to Details without saving | — |

**Tests covering Doctor:** `Doctor_CanCreateEditSaveMedicalRecord_WithValidation_AndRestrictions`, `Doctor_CannotSaveMedicalRecord_WithEmptyRequiredFields`

---

## Billing

| Form | Positive Case | Negative Case(s) |
|------|--------------|-----------------|
| **Create Invoice** | Add line item → saved, Details redirect | Empty line description → validation; no lines → server rejects |
| **Register Payment** | Amount=$50 → payment row appears, cancel button visible | Amount=$0 → server rejects (≤0 check), redirects with error |
| **Cancel Payment** | SweetAlert confirm → payment voided | SweetAlert dismiss → nothing happens |

**Tests covering Billing:** `Billing_CanCreateInvoice_RegisterPayment_CancelPayment_WithValidation`, `Billing_InvoiceCreate_ValidatesAndSavesCorrectly`

---

## Staff

| Form | Positive Case | Negative Case(s) |
|------|--------------|-----------------|
| **Create Appointment** | Fill required fields → success | — |
| **Edit Appointment** | Update room → saved | — |

**Tests covering Staff:** `Staff_CanCreateEditSaveCancelAppointment_And_IsRestrictedFromClinicalBilling`

---

## Patient (Portal)

| Form | Positive Case | Negative Case(s) |
|------|--------------|-----------------|
| **View Profile** | Profile data visible | — |
| **URL Bypass Attempts** | N/A | All staff routes → Access Denied |

**Tests covering Patient:** `Patient_Portal_CanViewProfile_And_BypassAttempts_AreBlocked`, `Patient_CanLogin_ViewDashboard_AndLogout`, `Patient_Portal_CannotAccess_AnyStaffRoute`
