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

            // DeletedAt e IsDeleted en Secretarias y Doctores ya fueron agregados
            // por la migración anterior SoftDeleteAndNullableEspecialidad (con IF NOT EXISTS).

            migrationBuilder.AlterColumn<int>(
                name: "EspecialidadId",
                table: "Doctores",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

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

            // DeletedAt e IsDeleted son responsabilidad de SoftDeleteAndNullableEspecialidad.

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
