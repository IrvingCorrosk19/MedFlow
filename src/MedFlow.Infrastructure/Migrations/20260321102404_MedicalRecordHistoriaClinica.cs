using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MedicalRecordHistoriaClinica : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MedicalRecords_Appointments_AppointmentId",
                table: "MedicalRecords");

            migrationBuilder.DropIndex(
                name: "IX_MedicalRecords_PatientId",
                table: "MedicalRecords");

            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "Prescriptions");

            migrationBuilder.RenameColumn(
                name: "Medication",
                table: "Prescriptions",
                newName: "MedicationName");

            migrationBuilder.RenameColumn(
                name: "RecordDate",
                table: "MedicalRecords",
                newName: "VisitDate");

            migrationBuilder.RenameColumn(
                name: "Notes",
                table: "MedicalRecords",
                newName: "ClinicalNotes");

            migrationBuilder.Sql("""
                UPDATE "MedicalRecords" SET "ClinicalNotes" =
                  CASE
                    WHEN (COALESCE(TRIM("VitalSigns"), '') <> '' OR COALESCE(TRIM("PhysicalExam"), '') <> '')
                    THEN (
                      COALESCE(NULLIF(TRIM("ClinicalNotes"), ''), '') ||
                      CASE WHEN COALESCE(TRIM("VitalSigns"), '') <> '' THEN E'\n\n--- Signos vitales ---\n' || TRIM("VitalSigns") ELSE '' END ||
                      CASE WHEN COALESCE(TRIM("PhysicalExam"), '') <> '' THEN E'\n\n--- Examen físico ---\n' || TRIM("PhysicalExam") ELSE '' END
                    )
                    ELSE "ClinicalNotes"
                  END
                WHERE COALESCE(TRIM("VitalSigns"), '') <> '' OR COALESCE(TRIM("PhysicalExam"), '') <> '';
                """);

            migrationBuilder.DropColumn(
                name: "PhysicalExam",
                table: "MedicalRecords");

            migrationBuilder.DropColumn(
                name: "VitalSigns",
                table: "MedicalRecords");

            migrationBuilder.AlterColumn<string>(
                name: "Instructions",
                table: "Prescriptions",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Frequency",
                table: "Prescriptions",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Duration",
                table: "Prescriptions",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Dosage",
                table: "Prescriptions",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TreatmentPlan",
                table: "MedicalRecords",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Observations",
                table: "MedicalRecords",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BloodPressure",
                table: "MedicalRecords",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ClinicalNotes",
                table: "MedicalRecords",
                type: "character varying(8000)",
                maxLength: 8000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HeartRateBpm",
                table: "MedicalRecords",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "HeightCm",
                table: "MedicalRecords",
                type: "numeric(6,2)",
                precision: 6,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TemperatureCelsius",
                table: "MedicalRecords",
                type: "numeric(4,1)",
                precision: 4,
                scale: 1,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "WeightKg",
                table: "MedicalRecords",
                type: "numeric(6,2)",
                precision: 6,
                scale: 2,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MedicalAttachments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MedicalRecordId = table.Column<Guid>(type: "uuid", nullable: false),
                    FileName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    FilePath = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    UploadedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicalAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MedicalAttachments_MedicalRecords_MedicalRecordId",
                        column: x => x.MedicalRecordId,
                        principalTable: "MedicalRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MedicalRecords_PatientId_VisitDate",
                table: "MedicalRecords",
                columns: new[] { "PatientId", "VisitDate" });

            migrationBuilder.CreateIndex(
                name: "IX_MedicalAttachments_MedicalRecordId",
                table: "MedicalAttachments",
                column: "MedicalRecordId");

            migrationBuilder.AddForeignKey(
                name: "FK_MedicalRecords_Appointments_AppointmentId",
                table: "MedicalRecords",
                column: "AppointmentId",
                principalTable: "Appointments",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MedicalRecords_Appointments_AppointmentId",
                table: "MedicalRecords");

            migrationBuilder.DropTable(
                name: "MedicalAttachments");

            migrationBuilder.DropIndex(
                name: "IX_MedicalRecords_PatientId_VisitDate",
                table: "MedicalRecords");

            migrationBuilder.DropColumn(
                name: "BloodPressure",
                table: "MedicalRecords");

            migrationBuilder.DropColumn(
                name: "HeartRateBpm",
                table: "MedicalRecords");

            migrationBuilder.DropColumn(
                name: "HeightCm",
                table: "MedicalRecords");

            migrationBuilder.DropColumn(
                name: "TemperatureCelsius",
                table: "MedicalRecords");

            migrationBuilder.DropColumn(
                name: "WeightKg",
                table: "MedicalRecords");

            migrationBuilder.AlterColumn<string>(
                name: "ClinicalNotes",
                table: "MedicalRecords",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(8000)",
                oldMaxLength: 8000,
                oldNullable: true);

            migrationBuilder.RenameColumn(
                name: "ClinicalNotes",
                table: "MedicalRecords",
                newName: "Notes");

            migrationBuilder.AddColumn<string>(
                name: "PhysicalExam",
                table: "MedicalRecords",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VitalSigns",
                table: "MedicalRecords",
                type: "text",
                nullable: true);

            migrationBuilder.RenameColumn(
                name: "VisitDate",
                table: "MedicalRecords",
                newName: "RecordDate");

            migrationBuilder.RenameColumn(
                name: "MedicationName",
                table: "Prescriptions",
                newName: "Medication");

            migrationBuilder.AlterColumn<string>(
                name: "Instructions",
                table: "Prescriptions",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Frequency",
                table: "Prescriptions",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Duration",
                table: "Prescriptions",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Dosage",
                table: "Prescriptions",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Quantity",
                table: "Prescriptions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "TreatmentPlan",
                table: "MedicalRecords",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(4000)",
                oldMaxLength: 4000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Observations",
                table: "MedicalRecords",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(4000)",
                oldMaxLength: 4000,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MedicalRecords_PatientId",
                table: "MedicalRecords",
                column: "PatientId");

            migrationBuilder.AddForeignKey(
                name: "FK_MedicalRecords_Appointments_AppointmentId",
                table: "MedicalRecords",
                column: "AppointmentId",
                principalTable: "Appointments",
                principalColumn: "Id");
        }
    }
}
