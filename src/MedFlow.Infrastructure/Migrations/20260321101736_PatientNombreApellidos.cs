using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PatientNombreApellidos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Patients_FirstName_LastName",
                table: "Patients");

            migrationBuilder.DropIndex(
                name: "IX_Patients_DocumentNumber",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "BloodType",
                table: "Patients");

            migrationBuilder.RenameColumn(
                name: "FirstName",
                table: "Patients",
                newName: "PrimerNombre");

            migrationBuilder.RenameColumn(
                name: "LastName",
                table: "Patients",
                newName: "PrimerApellido");

            migrationBuilder.RenameColumn(
                name: "DateOfBirth",
                table: "Patients",
                newName: "FechaNacimiento");

            migrationBuilder.RenameColumn(
                name: "Gender",
                table: "Patients",
                newName: "Sexo");

            migrationBuilder.RenameColumn(
                name: "DocumentType",
                table: "Patients",
                newName: "TipoDocumento");

            migrationBuilder.RenameColumn(
                name: "DocumentNumber",
                table: "Patients",
                newName: "NumeroDocumento");

            migrationBuilder.RenameColumn(
                name: "Phone",
                table: "Patients",
                newName: "Telefono");

            migrationBuilder.RenameColumn(
                name: "Email",
                table: "Patients",
                newName: "Correo");

            migrationBuilder.RenameColumn(
                name: "Address",
                table: "Patients",
                newName: "Direccion");

            migrationBuilder.RenameColumn(
                name: "EmergencyContactName",
                table: "Patients",
                newName: "ContactoEmergenciaNombre");

            migrationBuilder.RenameColumn(
                name: "EmergencyContactPhone",
                table: "Patients",
                newName: "ContactoEmergenciaTelefono");

            migrationBuilder.RenameColumn(
                name: "Allergies",
                table: "Patients",
                newName: "Alergias");

            migrationBuilder.RenameColumn(
                name: "Notes",
                table: "Patients",
                newName: "Observaciones");

            migrationBuilder.AlterColumn<string>(
                name: "TipoDocumento",
                table: "Patients",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Alergias",
                table: "Patients",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Observaciones",
                table: "Patients",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SegundoNombre",
                table: "Patients",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SegundoApellido",
                table: "Patients",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Patients_NumeroDocumento",
                table: "Patients",
                column: "NumeroDocumento");

            migrationBuilder.CreateIndex(
                name: "IX_Patients_PrimerApellido_PrimerNombre",
                table: "Patients",
                columns: new[] { "PrimerApellido", "PrimerNombre" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Patients_NumeroDocumento",
                table: "Patients");

            migrationBuilder.DropIndex(
                name: "IX_Patients_PrimerApellido_PrimerNombre",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "SegundoNombre",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "SegundoApellido",
                table: "Patients");

            migrationBuilder.RenameColumn(
                name: "PrimerNombre",
                table: "Patients",
                newName: "FirstName");

            migrationBuilder.RenameColumn(
                name: "PrimerApellido",
                table: "Patients",
                newName: "LastName");

            migrationBuilder.RenameColumn(
                name: "FechaNacimiento",
                table: "Patients",
                newName: "DateOfBirth");

            migrationBuilder.RenameColumn(
                name: "Sexo",
                table: "Patients",
                newName: "Gender");

            migrationBuilder.RenameColumn(
                name: "TipoDocumento",
                table: "Patients",
                newName: "DocumentType");

            migrationBuilder.RenameColumn(
                name: "NumeroDocumento",
                table: "Patients",
                newName: "DocumentNumber");

            migrationBuilder.RenameColumn(
                name: "Telefono",
                table: "Patients",
                newName: "Phone");

            migrationBuilder.RenameColumn(
                name: "Correo",
                table: "Patients",
                newName: "Email");

            migrationBuilder.RenameColumn(
                name: "Direccion",
                table: "Patients",
                newName: "Address");

            migrationBuilder.RenameColumn(
                name: "ContactoEmergenciaNombre",
                table: "Patients",
                newName: "EmergencyContactName");

            migrationBuilder.RenameColumn(
                name: "ContactoEmergenciaTelefono",
                table: "Patients",
                newName: "EmergencyContactPhone");

            migrationBuilder.RenameColumn(
                name: "Alergias",
                table: "Patients",
                newName: "Allergies");

            migrationBuilder.RenameColumn(
                name: "Observaciones",
                table: "Patients",
                newName: "Notes");

            migrationBuilder.AlterColumn<string>(
                name: "DocumentType",
                table: "Patients",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(30)",
                oldMaxLength: 30,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Allergies",
                table: "Patients",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                table: "Patients",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(4000)",
                oldMaxLength: 4000,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BloodType",
                table: "Patients",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Patients_DocumentNumber",
                table: "Patients",
                column: "DocumentNumber");

            migrationBuilder.CreateIndex(
                name: "IX_Patients_FirstName_LastName",
                table: "Patients",
                columns: new[] { "FirstName", "LastName" });
        }
    }
}
