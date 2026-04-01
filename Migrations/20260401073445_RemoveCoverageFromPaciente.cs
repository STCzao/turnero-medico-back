using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace turnero_medico_backend.Migrations
{
    /// <inheritdoc />
    public partial class RemoveCoverageFromPaciente : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Pacientes_ObrasSociales_ObraSocialId",
                table: "Pacientes");

            migrationBuilder.DropIndex(
                name: "IX_Pacientes_ObraSocialId",
                table: "Pacientes");

            migrationBuilder.DropColumn(
                name: "NumeroAfiliado",
                table: "Pacientes");

            migrationBuilder.DropColumn(
                name: "ObraSocialId",
                table: "Pacientes");

            migrationBuilder.DropColumn(
                name: "PlanAfiliado",
                table: "Pacientes");

            migrationBuilder.DropColumn(
                name: "TipoPago",
                table: "Pacientes");

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Pacientes",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Pacientes",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Pacientes");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Pacientes");

            migrationBuilder.AddColumn<string>(
                name: "NumeroAfiliado",
                table: "Pacientes",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "ObraSocialId",
                table: "Pacientes",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PlanAfiliado",
                table: "Pacientes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TipoPago",
                table: "Pacientes",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Pacientes_ObraSocialId",
                table: "Pacientes",
                column: "ObraSocialId");

            migrationBuilder.AddForeignKey(
                name: "FK_Pacientes_ObrasSociales_ObraSocialId",
                table: "Pacientes",
                column: "ObraSocialId",
                principalTable: "ObrasSociales",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
