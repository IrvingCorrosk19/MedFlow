using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedFlow.Infrastructure.Migrations;

/// <inheritdoc />
public partial class MultiTenantV2 : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_Appointments_Clinics_ClinicId",
            table: "Appointments");

        migrationBuilder.DropForeignKey(
            name: "FK_Doctors_Clinics_ClinicId",
            table: "Doctors");

        migrationBuilder.DropForeignKey(
            name: "FK_Patients_Clinics_ClinicId",
            table: "Patients");

        migrationBuilder.RenameTable(
            name: "Clinics",
            newName: "Tenants");

        migrationBuilder.AddColumn<string>(
            name: "Code",
            table: "Tenants",
            type: "character varying(80)",
            maxLength: 80,
            nullable: false,
            defaultValue: "demo");

        migrationBuilder.AddColumn<string>(
            name: "PrimaryColor",
            table: "Tenants",
            type: "character varying(32)",
            maxLength: 32,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "SecondaryColor",
            table: "Tenants",
            type: "character varying(32)",
            maxLength: 32,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "SubscriptionPlan",
            table: "Tenants",
            type: "character varying(64)",
            maxLength: 64,
            nullable: true);

        migrationBuilder.Sql(
            @"UPDATE ""Tenants"" SET ""Code"" = 'demo' WHERE ""Code"" IS NULL OR TRIM(""Code"") = '';");

        migrationBuilder.RenameColumn(
            name: "ClinicId",
            table: "Patients",
            newName: "TenantId");

        migrationBuilder.RenameColumn(
            name: "ClinicId",
            table: "Doctors",
            newName: "TenantId");

        migrationBuilder.RenameColumn(
            name: "ClinicId",
            table: "Appointments",
            newName: "TenantId");

        migrationBuilder.RenameColumn(
            name: "ClinicId",
            table: "AspNetUsers",
            newName: "TenantId");

        migrationBuilder.RenameIndex(
            name: "IX_Patients_ClinicId",
            table: "Patients",
            newName: "IX_Patients_TenantId");

        migrationBuilder.RenameIndex(
            name: "IX_Doctors_ClinicId",
            table: "Doctors",
            newName: "IX_Doctors_TenantId");

        migrationBuilder.RenameIndex(
            name: "IX_Appointments_ClinicId",
            table: "Appointments",
            newName: "IX_Appointments_TenantId");

        migrationBuilder.AddColumn<Guid>(
            name: "TenantId",
            table: "AspNetRoles",
            type: "uuid",
            nullable: true);

        migrationBuilder.DropIndex(
            name: "IX_BillingInvoices_InvoiceNumber",
            table: "BillingInvoices");

        migrationBuilder.AddColumn<Guid>(
            name: "TenantId",
            table: "MedicalRecords",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "TenantId",
            table: "BillingInvoices",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "TenantId",
            table: "BillingInvoiceItems",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "TenantId",
            table: "Payments",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "TenantId",
            table: "CashMovements",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "TenantId",
            table: "EventLogs",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "TenantId",
            table: "AuditLogs",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "TenantId",
            table: "Notifications",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "TenantId",
            table: "Prescriptions",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "TenantId",
            table: "MedicalAttachments",
            type: "uuid",
            nullable: true);

        migrationBuilder.Sql(
            """
            UPDATE "MedicalRecords" mr SET "TenantId" = p."TenantId" FROM "Patients" p WHERE mr."PatientId" = p."Id";
            UPDATE "BillingInvoices" bi SET "TenantId" = p."TenantId" FROM "Patients" p WHERE bi."PatientId" = p."Id";
            UPDATE "BillingInvoiceItems" li SET "TenantId" = inv."TenantId" FROM "BillingInvoices" inv WHERE li."BillingInvoiceId" = inv."Id";
            UPDATE "Payments" pay SET "TenantId" = inv."TenantId" FROM "BillingInvoices" inv WHERE pay."BillingInvoiceId" = inv."Id";
            UPDATE "CashMovements" cm SET "TenantId" = pay."TenantId" FROM "Payments" pay WHERE cm."RelatedPaymentId" IS NOT NULL AND cm."RelatedPaymentId" = pay."Id";
            UPDATE "Prescriptions" pr SET "TenantId" = mr."TenantId" FROM "MedicalRecords" mr WHERE pr."MedicalRecordId" = mr."Id";
            UPDATE "MedicalAttachments" ma SET "TenantId" = mr."TenantId" FROM "MedicalRecords" mr WHERE ma."MedicalRecordId" = mr."Id";
            UPDATE "EventLogs" SET "TenantId" = (SELECT "Id" FROM "Tenants" LIMIT 1) WHERE "TenantId" IS NULL;
            UPDATE "AuditLogs" SET "TenantId" = (SELECT "Id" FROM "Tenants" LIMIT 1) WHERE "TenantId" IS NULL;
            UPDATE "Notifications" SET "TenantId" = (SELECT "Id" FROM "Tenants" LIMIT 1) WHERE "TenantId" IS NULL;
            UPDATE "CashMovements" SET "TenantId" = (SELECT "Id" FROM "Tenants" LIMIT 1) WHERE "TenantId" IS NULL;
            UPDATE "MedicalRecords" SET "TenantId" = (SELECT "Id" FROM "Tenants" LIMIT 1) WHERE "TenantId" IS NULL;
            UPDATE "BillingInvoices" SET "TenantId" = (SELECT "Id" FROM "Tenants" LIMIT 1) WHERE "TenantId" IS NULL;
            UPDATE "BillingInvoiceItems" SET "TenantId" = (SELECT "Id" FROM "Tenants" LIMIT 1) WHERE "TenantId" IS NULL;
            UPDATE "Payments" SET "TenantId" = (SELECT "Id" FROM "Tenants" LIMIT 1) WHERE "TenantId" IS NULL;
            UPDATE "Prescriptions" SET "TenantId" = (SELECT "Id" FROM "Tenants" LIMIT 1) WHERE "TenantId" IS NULL;
            UPDATE "MedicalAttachments" SET "TenantId" = (SELECT "Id" FROM "Tenants" LIMIT 1) WHERE "TenantId" IS NULL;
            """);

        migrationBuilder.AlterColumn<Guid>(
            name: "TenantId",
            table: "MedicalRecords",
            type: "uuid",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "uuid",
            oldNullable: true);

        migrationBuilder.AlterColumn<Guid>(
            name: "TenantId",
            table: "BillingInvoices",
            type: "uuid",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "uuid",
            oldNullable: true);

        migrationBuilder.AlterColumn<Guid>(
            name: "TenantId",
            table: "BillingInvoiceItems",
            type: "uuid",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "uuid",
            oldNullable: true);

        migrationBuilder.AlterColumn<Guid>(
            name: "TenantId",
            table: "Payments",
            type: "uuid",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "uuid",
            oldNullable: true);

        migrationBuilder.AlterColumn<Guid>(
            name: "TenantId",
            table: "CashMovements",
            type: "uuid",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "uuid",
            oldNullable: true);

        migrationBuilder.AlterColumn<Guid>(
            name: "TenantId",
            table: "EventLogs",
            type: "uuid",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "uuid",
            oldNullable: true);

        migrationBuilder.AlterColumn<Guid>(
            name: "TenantId",
            table: "AuditLogs",
            type: "uuid",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "uuid",
            oldNullable: true);

        migrationBuilder.AlterColumn<Guid>(
            name: "TenantId",
            table: "Notifications",
            type: "uuid",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "uuid",
            oldNullable: true);

        migrationBuilder.AlterColumn<Guid>(
            name: "TenantId",
            table: "Prescriptions",
            type: "uuid",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "uuid",
            oldNullable: true);

        migrationBuilder.AlterColumn<Guid>(
            name: "TenantId",
            table: "MedicalAttachments",
            type: "uuid",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "uuid",
            oldNullable: true);

        migrationBuilder.CreateTable(
            name: "TenantSettings",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                SettingKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                SettingValue = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: true),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_TenantSettings", x => x.Id);
                table.ForeignKey(
                    name: "FK_TenantSettings_Tenants_TenantId",
                    column: x => x.TenantId,
                    principalTable: "Tenants",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Prescriptions_TenantId",
            table: "Prescriptions",
            column: "TenantId");

        migrationBuilder.CreateIndex(
            name: "IX_Payments_TenantId",
            table: "Payments",
            column: "TenantId");

        migrationBuilder.CreateIndex(
            name: "IX_Payments_TenantId_PaymentDate",
            table: "Payments",
            columns: new[] { "TenantId", "PaymentDate" });

        migrationBuilder.CreateIndex(
            name: "IX_Patients_TenantId_CreatedAt",
            table: "Patients",
            columns: new[] { "TenantId", "CreatedAt" });

        migrationBuilder.CreateIndex(
            name: "IX_Notifications_TenantId",
            table: "Notifications",
            column: "TenantId");

        migrationBuilder.CreateIndex(
            name: "IX_MedicalRecords_TenantId",
            table: "MedicalRecords",
            column: "TenantId");

        migrationBuilder.CreateIndex(
            name: "IX_MedicalRecords_TenantId_VisitDate",
            table: "MedicalRecords",
            columns: new[] { "TenantId", "VisitDate" });

        migrationBuilder.CreateIndex(
            name: "IX_MedicalAttachments_TenantId",
            table: "MedicalAttachments",
            column: "TenantId");

        migrationBuilder.CreateIndex(
            name: "IX_EventLogs_TenantId",
            table: "EventLogs",
            column: "TenantId");

        migrationBuilder.CreateIndex(
            name: "IX_EventLogs_TenantId_Status_CreatedAt",
            table: "EventLogs",
            columns: new[] { "TenantId", "Status", "CreatedAt" });

        migrationBuilder.CreateIndex(
            name: "IX_Doctors_TenantId_LastName",
            table: "Doctors",
            columns: new[] { "TenantId", "LastName" });

        migrationBuilder.CreateIndex(
            name: "IX_CashMovements_TenantId",
            table: "CashMovements",
            column: "TenantId");

        migrationBuilder.CreateIndex(
            name: "IX_CashMovements_TenantId_MovementDate",
            table: "CashMovements",
            columns: new[] { "TenantId", "MovementDate" });

        migrationBuilder.CreateIndex(
            name: "IX_BillingInvoices_TenantId",
            table: "BillingInvoices",
            column: "TenantId");

        migrationBuilder.CreateIndex(
            name: "IX_BillingInvoices_TenantId_IssueDate",
            table: "BillingInvoices",
            columns: new[] { "TenantId", "IssueDate" });

        migrationBuilder.CreateIndex(
            name: "IX_BillingInvoices_TenantId_InvoiceNumber",
            table: "BillingInvoices",
            columns: new[] { "TenantId", "InvoiceNumber" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_BillingInvoiceItems_TenantId",
            table: "BillingInvoiceItems",
            column: "TenantId");

        migrationBuilder.CreateIndex(
            name: "IX_AuditLogs_TenantId",
            table: "AuditLogs",
            column: "TenantId");

        migrationBuilder.CreateIndex(
            name: "IX_AuditLogs_TenantId_CreatedAt",
            table: "AuditLogs",
            columns: new[] { "TenantId", "CreatedAt" });

        migrationBuilder.CreateIndex(
            name: "IX_AspNetUsers_TenantId",
            table: "AspNetUsers",
            column: "TenantId");

        migrationBuilder.CreateIndex(
            name: "IX_AspNetRoles_TenantId",
            table: "AspNetRoles",
            column: "TenantId");

        migrationBuilder.CreateIndex(
            name: "IX_Appointments_TenantId_ScheduledDate_Status",
            table: "Appointments",
            columns: new[] { "TenantId", "ScheduledDate", "Status" });

        migrationBuilder.CreateIndex(
            name: "IX_Tenants_Code",
            table: "Tenants",
            column: "Code",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_TenantSettings_TenantId_SettingKey",
            table: "TenantSettings",
            columns: new[] { "TenantId", "SettingKey" },
            unique: true);

        migrationBuilder.AddForeignKey(
            name: "FK_Appointments_Tenants_TenantId",
            table: "Appointments",
            column: "TenantId",
            principalTable: "Tenants",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_AspNetRoles_Tenants_TenantId",
            table: "AspNetRoles",
            column: "TenantId",
            principalTable: "Tenants",
            principalColumn: "Id",
            onDelete: ReferentialAction.SetNull);

        migrationBuilder.AddForeignKey(
            name: "FK_AspNetUsers_Tenants_TenantId",
            table: "AspNetUsers",
            column: "TenantId",
            principalTable: "Tenants",
            principalColumn: "Id",
            onDelete: ReferentialAction.SetNull);

        migrationBuilder.AddForeignKey(
            name: "FK_AuditLogs_Tenants_TenantId",
            table: "AuditLogs",
            column: "TenantId",
            principalTable: "Tenants",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_BillingInvoiceItems_Tenants_TenantId",
            table: "BillingInvoiceItems",
            column: "TenantId",
            principalTable: "Tenants",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_BillingInvoices_Tenants_TenantId",
            table: "BillingInvoices",
            column: "TenantId",
            principalTable: "Tenants",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_CashMovements_Tenants_TenantId",
            table: "CashMovements",
            column: "TenantId",
            principalTable: "Tenants",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_Doctors_Tenants_TenantId",
            table: "Doctors",
            column: "TenantId",
            principalTable: "Tenants",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_EventLogs_Tenants_TenantId",
            table: "EventLogs",
            column: "TenantId",
            principalTable: "Tenants",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_MedicalAttachments_Tenants_TenantId",
            table: "MedicalAttachments",
            column: "TenantId",
            principalTable: "Tenants",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_MedicalRecords_Tenants_TenantId",
            table: "MedicalRecords",
            column: "TenantId",
            principalTable: "Tenants",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_Notifications_Tenants_TenantId",
            table: "Notifications",
            column: "TenantId",
            principalTable: "Tenants",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);

        migrationBuilder.AddForeignKey(
            name: "FK_Patients_Tenants_TenantId",
            table: "Patients",
            column: "TenantId",
            principalTable: "Tenants",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_Payments_Tenants_TenantId",
            table: "Payments",
            column: "TenantId",
            principalTable: "Tenants",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_Prescriptions_Tenants_TenantId",
            table: "Prescriptions",
            column: "TenantId",
            principalTable: "Tenants",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        throw new NotSupportedException("Migración multi-tenant irreversible sin script manual.");
    }
}
