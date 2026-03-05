using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace turnero_medico_backend.Migrations
{
    /// <inheritdoc />
    public partial class RefactorTurnoFlowAndSecretaria : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ValidadoPorDoctorId",
                table: "Turnos",
                newName: "PlanAfiliadoDeclarado");

            migrationBuilder.RenameColumn(
                name: "NotasFacturacion",
                table: "Turnos",
                newName: "Especialidad");

            migrationBuilder.RenameColumn(
                name: "FechaValidacion",
                table: "Turnos",
                newName: "FechaGestion");

            migrationBuilder.AlterColumn<DateTime>(
                name: "FechaHora",
                table: "Turnos",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<int>(
                name: "DoctorId",
                table: "Turnos",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<string>(
                name: "ConfirmadaPorId",
                table: "Turnos",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NotasSecretaria",
                table: "Turnos",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NumeroAfiliadoDeclarado",
                table: "Turnos",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ObservacionClinica",
                table: "Turnos",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Turnos",
                type: "bytea",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PlanAfiliado",
                table: "Pacientes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Observaciones",
                table: "ObrasSociales",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<List<string>>(
                name: "Planes",
                table: "ObrasSociales",
                type: "jsonb",
                nullable: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConfirmadaPorId",
                table: "Turnos");

            migrationBuilder.DropColumn(
                name: "NotasSecretaria",
                table: "Turnos");

            migrationBuilder.DropColumn(
                name: "NumeroAfiliadoDeclarado",
                table: "Turnos");

            migrationBuilder.DropColumn(
                name: "ObservacionClinica",
                table: "Turnos");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Turnos");

            migrationBuilder.DropColumn(
                name: "PlanAfiliado",
                table: "Pacientes");

            migrationBuilder.DropColumn(
                name: "Observaciones",
                table: "ObrasSociales");

            migrationBuilder.DropColumn(
                name: "Planes",
                table: "ObrasSociales");

            migrationBuilder.RenameColumn(
                name: "PlanAfiliadoDeclarado",
                table: "Turnos",
                newName: "ValidadoPorDoctorId");

            migrationBuilder.RenameColumn(
                name: "FechaGestion",
                table: "Turnos",
                newName: "FechaValidacion");

            migrationBuilder.RenameColumn(
                name: "Especialidad",
                table: "Turnos",
                newName: "NotasFacturacion");

            migrationBuilder.AlterColumn<DateTime>(
                name: "FechaHora",
                table: "Turnos",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "DoctorId",
                table: "Turnos",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);
        }
    }
}
