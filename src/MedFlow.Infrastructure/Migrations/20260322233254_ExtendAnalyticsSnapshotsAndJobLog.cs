using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ExtendAnalyticsSnapshotsAndJobLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ActiveDoctorsCount",
                table: "TenantDailySnapshots",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ActivePatientsCount",
                table: "TenantDailySnapshots",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ActiveUsersCount",
                table: "TenantDailySnapshots",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "AppointmentsConfirmedCount",
                table: "TenantDailySnapshots",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "AppointmentsCreatedCount",
                table: "TenantDailySnapshots",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "InvoicesPendingCount",
                table: "TenantDailySnapshots",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "NewPatientsCount",
                table: "TenantDailySnapshots",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "NotificationsFailedCount",
                table: "TenantDailySnapshots",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "NotificationsSentCount",
                table: "TenantDailySnapshots",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "PlanCode",
                table: "TenantDailySnapshots",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubscriptionStatus",
                table: "TenantDailySnapshots",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalInvoicedAmount",
                table: "TenantDailySnapshots",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "TenantDailySnapshots",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AnalyticsJobLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JobType = table.Column<string>(type: "text", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    SnapshotDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnalyticsJobLogs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AnalyticsJobLogs_JobType_CreatedAt",
                table: "AnalyticsJobLogs",
                columns: new[] { "JobType", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AnalyticsJobLogs_SnapshotDate",
                table: "AnalyticsJobLogs",
                column: "SnapshotDate");

            migrationBuilder.CreateIndex(
                name: "IX_AnalyticsJobLogs_TenantId",
                table: "AnalyticsJobLogs",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AnalyticsJobLogs");

            migrationBuilder.DropColumn(
                name: "ActiveDoctorsCount",
                table: "TenantDailySnapshots");

            migrationBuilder.DropColumn(
                name: "ActivePatientsCount",
                table: "TenantDailySnapshots");

            migrationBuilder.DropColumn(
                name: "ActiveUsersCount",
                table: "TenantDailySnapshots");

            migrationBuilder.DropColumn(
                name: "AppointmentsConfirmedCount",
                table: "TenantDailySnapshots");

            migrationBuilder.DropColumn(
                name: "AppointmentsCreatedCount",
                table: "TenantDailySnapshots");

            migrationBuilder.DropColumn(
                name: "InvoicesPendingCount",
                table: "TenantDailySnapshots");

            migrationBuilder.DropColumn(
                name: "NewPatientsCount",
                table: "TenantDailySnapshots");

            migrationBuilder.DropColumn(
                name: "NotificationsFailedCount",
                table: "TenantDailySnapshots");

            migrationBuilder.DropColumn(
                name: "NotificationsSentCount",
                table: "TenantDailySnapshots");

            migrationBuilder.DropColumn(
                name: "PlanCode",
                table: "TenantDailySnapshots");

            migrationBuilder.DropColumn(
                name: "SubscriptionStatus",
                table: "TenantDailySnapshots");

            migrationBuilder.DropColumn(
                name: "TotalInvoicedAmount",
                table: "TenantDailySnapshots");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "TenantDailySnapshots");
        }
    }
}
