using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAnalyticsSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TenantDailySnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    SnapshotDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AppointmentsScheduled = table.Column<int>(type: "integer", nullable: false),
                    AppointmentsCompleted = table.Column<int>(type: "integer", nullable: false),
                    AppointmentsCancelled = table.Column<int>(type: "integer", nullable: false),
                    AppointmentsNoShow = table.Column<int>(type: "integer", nullable: false),
                    AppointmentsRescheduled = table.Column<int>(type: "integer", nullable: false),
                    AppointmentsTotal = table.Column<int>(type: "integer", nullable: false),
                    PatientsTotal = table.Column<int>(type: "integer", nullable: false),
                    PatientsNewInPeriod = table.Column<int>(type: "integer", nullable: false),
                    DoctorsActive = table.Column<int>(type: "integer", nullable: false),
                    MedicalRecordsCreated = table.Column<int>(type: "integer", nullable: false),
                    InvoicesCreated = table.Column<int>(type: "integer", nullable: false),
                    InvoicesPaid = table.Column<int>(type: "integer", nullable: false),
                    InvoicesOverdue = table.Column<int>(type: "integer", nullable: false),
                    RevenueCollected = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    BalanceDueTotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    PaymentsCount = table.Column<int>(type: "integer", nullable: false),
                    AIInsightsGenerated = table.Column<int>(type: "integer", nullable: false),
                    AIInsightsCritical = table.Column<int>(type: "integer", nullable: false),
                    AIInsightsNew = table.Column<int>(type: "integer", nullable: false),
                    WorkflowExecutionsTotal = table.Column<int>(type: "integer", nullable: false),
                    WorkflowExecutionsSuccess = table.Column<int>(type: "integer", nullable: false),
                    WorkflowExecutionsFailed = table.Column<int>(type: "integer", nullable: false),
                    AppointmentsCompletedLast7 = table.Column<int>(type: "integer", nullable: false),
                    AppointmentsCompletedLast30 = table.Column<int>(type: "integer", nullable: false),
                    RevenueLast7 = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    RevenueLast30 = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantDailySnapshots", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TenantDailySnapshots_SnapshotDate",
                table: "TenantDailySnapshots",
                column: "SnapshotDate");

            migrationBuilder.CreateIndex(
                name: "IX_TenantDailySnapshots_TenantId_SnapshotDate",
                table: "TenantDailySnapshots",
                columns: new[] { "TenantId", "SnapshotDate" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TenantDailySnapshots");
        }
    }
}
