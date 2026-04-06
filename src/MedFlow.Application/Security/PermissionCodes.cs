namespace MedFlow.Application.Security;

public static class PermissionCodes
{
    public const string PatientsView = "patients.view";
    public const string PatientsCreate = "patients.create";
    public const string PatientsEdit = "patients.edit";
    public const string PatientsDelete = "patients.delete";

    public const string DoctorsView = "doctors.view";
    public const string DoctorsCreate = "doctors.create";
    public const string DoctorsEdit = "doctors.edit";
    public const string DoctorsDelete = "doctors.delete";

    public const string AppointmentsView = "appointments.view";
    public const string AppointmentsCreate = "appointments.create";
    public const string AppointmentsEdit = "appointments.edit";
    public const string AppointmentsCancel = "appointments.cancel";

    public const string MedicalRecordsView = "medical_records.view";
    public const string MedicalRecordsCreate = "medical_records.create";
    public const string MedicalRecordsEdit = "medical_records.edit";
    public const string MedicalRecordsDelete = "medical_records.delete";

    public const string BillingView = "billing.view";
    public const string BillingCreate = "billing.create";
    public const string BillingCancel = "billing.cancel";
    public const string BillingRegisterPayment = "billing.register_payment";
    public const string BillingManage = "billing.manage";

    public const string PaymentsCreate = "payments.create";

    public const string CashView = "cash.view";
    public const string CashCreate = "cash.create";
    public const string CashDelete = "cash.delete";

    public const string UsersManage = "users.manage";
    public const string RolesManage = "roles.manage";
    public const string PermissionsView = "permissions.view";
    public const string AuditView = "audit.view";

    public const string DashboardView = "dashboard.view";
    public const string ReportsView = "reports.view";
    public const string SettingsManage = "settings.manage";
    public const string AutomationsView = "automations.view";
    public const string AutomationsManage = "automations.manage";

    public const string EventLogsView = "event_logs.view";
    public const string AIInsightsView = "ai.insights.view";
    public const string AIInsightsManage = "ai.insights.manage";

    // Contabilidad
    public const string AccountingView   = "accounting.view";
    public const string AccountingManage = "accounting.manage";
    public const string AccountingPost   = "accounting.post";
    public const string AccountingClose  = "accounting.close";

    public static IReadOnlyList<(string Code, string Name, string Module)> All =>
    [
        (PatientsView, "Ver pacientes", "Patients"),
        (PatientsCreate, "Crear pacientes", "Patients"),
        (PatientsEdit, "Editar pacientes", "Patients"),
        (PatientsDelete, "Eliminar pacientes", "Patients"),
        (DoctorsView, "Ver doctores", "Doctors"),
        (DoctorsCreate, "Crear doctores", "Doctors"),
        (DoctorsEdit, "Editar doctores", "Doctors"),
        (DoctorsDelete, "Eliminar doctores", "Doctors"),
        (AppointmentsView, "Ver citas", "Appointments"),
        (AppointmentsCreate, "Crear citas", "Appointments"),
        (AppointmentsEdit, "Editar citas", "Appointments"),
        (AppointmentsCancel, "Cancelar citas", "Appointments"),
        (MedicalRecordsView, "Ver historias", "MedicalRecords"),
        (MedicalRecordsCreate, "Crear historias", "MedicalRecords"),
        (MedicalRecordsEdit, "Editar historias", "MedicalRecords"),
        (MedicalRecordsDelete, "Eliminar historias", "MedicalRecords"),
        (BillingView, "Ver facturación", "Billing"),
        (BillingCreate, "Crear facturas", "Billing"),
        (BillingCancel, "Cancelar facturas", "Billing"),
        (BillingRegisterPayment, "Registrar pagos", "Billing"),
        (BillingManage, "Gestionar facturación", "Billing"),
        (PaymentsCreate, "Crear pagos", "Billing"),
        (CashView, "Ver caja", "Cash"),
        (CashCreate, "Registrar movimientos de caja", "Cash"),
        (CashDelete, "Eliminar movimientos de caja", "Cash"),
        (UsersManage, "Gestionar usuarios", "Security"),
        (RolesManage, "Gestionar roles", "Security"),
        (PermissionsView, "Ver permisos", "Security"),
        (AuditView, "Ver auditoría", "Security"),
        (DashboardView, "Dashboard", "Core"),
        (ReportsView, "Reportes e indicadores", "Reports"),
        (SettingsManage, "Configuración", "Core"),
        (AutomationsView, "Ver automatizaciones", "Core"),
        (AutomationsManage, "Gestionar automatizaciones", "Core"),
        (EventLogsView, "Eventos del sistema", "Core"),
        (AIInsightsView, "Ver insights de IA", "AI"),
        (AIInsightsManage, "Gestionar insights de IA", "AI"),
        (AccountingView,   "Ver contabilidad", "Accounting"),
        (AccountingManage, "Gestionar contabilidad (cuentas, asientos)", "Accounting"),
        (AccountingPost,   "Publicar asientos contables", "Accounting"),
        (AccountingClose,  "Cerrar períodos fiscales", "Accounting"),
    ];
}
