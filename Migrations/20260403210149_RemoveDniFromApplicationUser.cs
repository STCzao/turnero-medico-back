using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace turnero_medico_backend.Migrations
{
    /// <inheritdoc />
    public partial class RemoveDniFromApplicationUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Doctores_Especialidades_EspecialidadId",
                table: "Doctores");

            migrationBuilder.DropForeignKey(
                name: "FK_Turnos_Doctores_DoctorId",
                table: "Turnos");

            migrationBuilder.DropForeignKey(
                name: "FK_Turnos_Especialidades_EspecialidadId",
                table: "Turnos");

            migrationBuilder.DropColumn(
                name: "Dni",
                table: "AspNetUsers");

            migrationBuilder.AlterColumn<int>(
                name: "EspecialidadId",
                table: "Turnos",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Secretarias",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Secretarias",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<int>(
                name: "EspecialidadId",
                table: "Doctores",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Doctores",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Doctores",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddForeignKey(
                name: "FK_Doctores_Especialidades_EspecialidadId",
                table: "Doctores",
                column: "EspecialidadId",
                principalTable: "Especialidades",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Turnos_Doctores_DoctorId",
                table: "Turnos",
                column: "DoctorId",
                principalTable: "Doctores",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Turnos_Especialidades_EspecialidadId",
                table: "Turnos",
                column: "EspecialidadId",
                principalTable: "Especialidades",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Doctores_Especialidades_EspecialidadId",
                table: "Doctores");

            migrationBuilder.DropForeignKey(
                name: "FK_Turnos_Doctores_DoctorId",
                table: "Turnos");

            migrationBuilder.DropForeignKey(
                name: "FK_Turnos_Especialidades_EspecialidadId",
                table: "Turnos");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Secretarias");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Secretarias");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Doctores");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Doctores");

            migrationBuilder.AlterColumn<int>(
                name: "EspecialidadId",
                table: "Turnos",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "EspecialidadId",
                table: "Doctores",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Dni",
                table: "AspNetUsers",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddForeignKey(
                name: "FK_Doctores_Especialidades_EspecialidadId",
                table: "Doctores",
                column: "EspecialidadId",
                principalTable: "Especialidades",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Turnos_Doctores_DoctorId",
                table: "Turnos",
                column: "DoctorId",
                principalTable: "Doctores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Turnos_Especialidades_EspecialidadId",
                table: "Turnos",
                column: "EspecialidadId",
                principalTable: "Especialidades",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
