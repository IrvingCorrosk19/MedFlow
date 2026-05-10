using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SyncPrescriptionColumnsWithDomain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsVoid",
                table: "Prescriptions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "IssuedAt",
                table: "Prescriptions",
                type: "timestamp without time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.Sql(@"UPDATE ""Prescriptions"" SET ""IssuedAt"" = ""CreatedAt"";");

            migrationBuilder.AddColumn<string>(
                name: "PrescriberLicense",
                table: "Prescriptions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PrescriberName",
                table: "Prescriptions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PrintCount",
                table: "Prescriptions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "VoidReason",
                table: "Prescriptions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedByUserId",
                table: "JournalEntries",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsVoid",
                table: "Prescriptions");

            migrationBuilder.DropColumn(
                name: "IssuedAt",
                table: "Prescriptions");

            migrationBuilder.DropColumn(
                name: "PrescriberLicense",
                table: "Prescriptions");

            migrationBuilder.DropColumn(
                name: "PrescriberName",
                table: "Prescriptions");

            migrationBuilder.DropColumn(
                name: "PrintCount",
                table: "Prescriptions");

            migrationBuilder.DropColumn(
                name: "VoidReason",
                table: "Prescriptions");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "JournalEntries");
        }
    }
}
