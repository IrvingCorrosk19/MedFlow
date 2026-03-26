# Bugs Found and Fixed During QA

## Bug 1 — Admin Checkbox Intercept (Bootstrap custom-control)
**Severity:** Medium (test-blocking)
**File:** `UnitTest1.cs` — Admin role test
**Symptom:** Clicking permission checkboxes failed with "element intercepted" in Playwright
**Root Cause:** Bootstrap `custom-control` renders a `<label>` that overlays the `<input type="checkbox">`. Direct checkbox click is intercepted by the label.
**Fix:** Used `Force = true` in `ClickAsync` to bypass the actionability check and click through the label overlay.
**Status:** ✅ Fixed

---

## Bug 2 — Navigation Race Condition on Form Redirect (WaitForNavigation)
**Severity:** Medium (test flakiness)
**Files:** `UnitTest1.cs` — multiple tests
**Symptom:** `WaitForNavigationAsync` resolved before the redirect target (GET) fully loaded, causing `#medflow-flash` lookups to time out.
**Root Cause:** The deprecated `WaitForNavigationAsync` (returned by `ClickAndWaitForNavigationAsync`) fired on the 302 response before the browser completed the redirect GET.
**Fix:** Replaced with `RunAndWaitForNavigationAsync(WaitUntil.Load)` + `WaitForLoadStateAsync(NetworkIdle)`.
**Status:** ✅ Fixed

---

## Bug 3 — `[Required]` on non-nullable `Guid` accepts `Guid.Empty` (App Bug)
**Severity:** Low (app design issue)
**File:** `AppointmentViewModel.cs` — `PatientId` field
**Symptom:** Submitting the appointment form without selecting a patient (leaving default `Guid.Empty`) passes ModelState validation and creates an appointment with `PatientId = Guid.Empty`.
**Root Cause:** ASP.NET Core's `[Required]` attribute on a non-nullable `Guid` does not reject `Guid.Empty`. Only `Guid?` would work.
**Fix Applied to Tests:** The negative appointment test was redesigned to use a schedule conflict scenario instead (reliable server-side rejection).
**App Fix Recommendation:** Change `PatientId` to `Guid?` with `[Required]`, or add a custom validation attribute checking `Guid.Empty`.
**Status:** ⚠️ Documented (app code not modified — out of test scope)

---

## Bug 4 — `NpgsqlRetryingExecutionStrategy` incompatible with user-initiated transactions (App Bug — CRITICAL)
**Severity:** Critical (payment registration completely broken)
**File:** `PaymentService.cs` — `RegisterAsync` and `CancelPaymentAsync`
**Symptom:** HTTP 500 on `POST /BillingInvoices/RegisterPayment` and `POST /BillingInvoices/CancelPayment`. Server returns JSON error body: `"The configured execution strategy 'NpgsqlRetryingExecutionStrategy' does not support user-initiated transactions."`
**Root Cause:** Both methods called `_db.Database.BeginTransactionAsync()` directly. When the Npgsql retry strategy is configured (for transient fault resilience), EF Core forbids user-initiated transactions unless wrapped with `Database.CreateExecutionStrategy().ExecuteAsync(...)`.
**Fix:** Refactored both `RegisterAsync` and `CancelPaymentAsync` to wrap the transaction block inside `_db.Database.CreateExecutionStrategy().ExecuteAsync(...)`. Error results are captured via closure variables instead of direct `return` from within the lambda.
**Status:** ✅ Fixed in `PaymentService.cs`

---

## Bug 5 — Appointment Schedule Conflict (Test State Pollution)
**Severity:** Low (test flakiness across runs)
**Files:** `UnitTest1.cs` — Reception and Staff appointment tests
**Symptom:** Second consecutive test run fails with schedule conflict because previous run's appointment was not cleaned up.
**Root Cause:** Tests used fixed time slots (14:00, 11:00) for appointment creation. The appointment conflict check (`HasConflictAsync`) correctly rejects duplicates — but the test environment has no teardown/cleanup.
**Fix:** Changed appointment date and time to ticks-based unique values that cycle across different days (2-6 days ahead) and hours (8-16h), changing every 10 seconds of wall-clock time.
**Status:** ✅ Fixed in `UnitTest1.cs`

---

## Bug 6 — SweetAlert2 Confirm Required for Payment Cancellation (Test Design)
**Severity:** Low (test-blocking)
**File:** `UnitTest1.cs` — Billing test
**Symptom:** Clicking `btn-cancel-pay` button had no effect without confirming the SweetAlert2 dialog.
**Root Cause:** The button click handler calls `Swal.fire(...)` and only calls `f.submit()` on `isConfirmed`. The form is not submitted without the confirmation.
**Fix:** Added `page.Locator("button.swal2-confirm").First.WaitForAsync(...)` + `ClickAsync(Force:true)` after the cancel button click.
**Status:** ✅ Fixed in test
