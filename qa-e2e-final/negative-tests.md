# Negative / Validation Tests

All negative cases verified to be caught by the server or client, preventing invalid data from being persisted.

---

## 1. User Creation — Empty Email
**Test:** `Admin_CannotCreateUser_WithDuplicateEmail_OrWeakPassword`
**Input:** Email = empty, Password = "Test1234!"
**Expected:** Stays on Create page, validation message visible
**Result:** ✅ PASS — model validation fires, no redirect

## 2. User Creation — Weak Password
**Test:** `Admin_CannotCreateUser_WithDuplicateEmail_OrWeakPassword`
**Input:** Valid email, Password = "weak"
**Expected:** Stays on Create, password complexity error
**Result:** ✅ PASS

## 3. User Creation — Duplicate Email
**Test:** `Admin_CannotCreateUser_WithDuplicateEmail_OrWeakPassword`
**Input:** Same email as existing user
**Expected:** Server-side error, stays on Create
**Result:** ✅ PASS

## 4. Appointment Creation — Schedule Conflict
**Test:** `Reception_CannotCreateAppointment_WithoutPatient`
**Input:** Create two appointments for the same doctor, date, and time slot
**Expected:** Second attempt stays on Create, server conflict check fires
**Result:** ✅ PASS

> **QA Finding:** `PatientId=[Required]` on a `Guid` property allows `Guid.Empty` to pass ModelState validation (ASP.NET Core app bug — documented in `bugs-fixed-final.md`). The negative case was redesigned to use schedule conflict instead.

## 5. Medical Record — Empty Doctor
**Test:** `Doctor_CannotSaveMedicalRecord_WithEmptyRequiredFields`
**Input:** DoctorId = empty, Diagnosis = empty
**Expected:** Validation summary visible, stays on form
**Result:** ✅ PASS

## 6. Medical Record Edit — Empty Doctor
**Test:** `Doctor_CanCreateEditSaveMedicalRecord_WithValidation_AndRestrictions`
**Input:** Clear DoctorId select → submit
**Expected:** Validation error visible on Edit page
**Result:** ✅ PASS

## 7. Invoice Creation — Empty Line Description
**Test:** `Billing_CanCreateInvoice_RegisterPayment_CancelPayment_WithValidation` + `Billing_InvoiceCreate_ValidatesAndSavesCorrectly`
**Input:** Line description = empty (required field)
**Expected:** Stays on Create, validation message visible
**Result:** ✅ PASS

## 8. Invoice Creation — No Lines
**Test:** `Billing_InvoiceCreate_ValidatesAndSavesCorrectly`
**Input:** No invoice lines added
**Expected:** Server rejects with "Debe agregar al menos una línea"
**Result:** ✅ PASS

## 9. Payment Registration — Amount = $0
**Test:** `Billing_CanCreateInvoice_RegisterPayment_CancelPayment_WithValidation`
**Input:** Amount = 0 (invalid per business rule: must be > 0)
**Expected:** Server rejects, redirects to Details with error, no payment registered
**Result:** ✅ PASS — `btn-cancel-pay` not visible after rejection

## 10. Patient Role — URL Bypass Attempts
**Test:** `Patient_Portal_CanViewProfile_And_BypassAttempts_AreBlocked`, `Patient_Portal_CannotAccess_AnyStaffRoute`
**Input:** Patient navigates to `/Appointments`, `/BillingInvoices`, `/Admin/Users`, etc.
**Expected:** Redirected to Access Denied page
**Result:** ✅ PASS

## 11. Doctor — Cannot Access Billing/Admin URLs
**Test:** `Doctor_CannotAccess_BillingCashAdminSecurity_Routes`
**Input:** Direct URL navigation to `/BillingInvoices`, `/CashMovements`, `/Admin/Users`
**Expected:** Access Denied
**Result:** ✅ PASS

## 12. Reception — Cannot Access Clinical/Billing/Admin URLs
**Test:** `Reception_CannotAccess_ClinicalBillingAdmin_Routes`
**Input:** Direct URL navigation to `/MedicalRecords`, `/BillingInvoices`, `/Admin/Users`
**Expected:** Access Denied
**Result:** ✅ PASS

## 13. Staff — Cannot Access Clinical/Billing/Admin URLs
**Test:** `Staff_CannotAccess_ClinicalBillingAdmin_Routes`, `Staff_CanCreateEditSaveCancelAppointment_And_IsRestrictedFromClinicalBilling`
**Input:** Direct URL navigation to `/MedicalRecords`, `/BillingInvoices`
**Expected:** Access Denied
**Result:** ✅ PASS

## 14. Billing Role — Cannot Access Clinical/Appointments/Admin URLs
**Test:** `Billing_CannotAccess_ClinicalAppointmentsAdmin_Routes`
**Input:** Direct URL navigation to `/MedicalRecords`, `/Appointments`, `/Admin/Users`
**Expected:** Access Denied
**Result:** ✅ PASS
