using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using MedFlow.Infrastructure.Persistence;

#nullable disable

namespace MedFlow.Infrastructure.Migrations;

/// <summary>Adds columns introduced on <see cref="MedFlow.Domain.Entities.FiscalPeriod"/> for yearly close tracking.</summary>
[DbContext(typeof(ApplicationDbContext))]
[Migration("20260510161000_AddFiscalPeriodYearlyCloseColumns")]
public partial class AddFiscalPeriodYearlyCloseColumns : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "IsYearlyClosed",
            table: "FiscalPeriods",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<DateTime>(
            name: "YearlyClosedAt",
            table: "FiscalPeriods",
            type: "timestamp without time zone",
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "YearlyClosedAt",
            table: "FiscalPeriods");

        migrationBuilder.DropColumn(
            name: "IsYearlyClosed",
            table: "FiscalPeriods");
    }
}
