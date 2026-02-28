using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace turnero_medico_backend.Migrations
{
    /// <inheritdoc />
    public partial class AddFamiliaAndObraSocialModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Turnos",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "CreatedByUserId",
                table: "Turnos",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NotasFacturacion",
                table: "Turnos",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "ObraSocialId",
                table: "Turnos",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EsMayorDeEdad",
                table: "Pacientes",
                type: "boolean",
                nullable: false,
                defaultValue: false);

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
                name: "ResponsableId",
                table: "Pacientes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TipoPago",
                table: "Pacientes",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "ObrasSociales",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "text", nullable: false),
                    Cobertura = table.Column<string>(type: "text", nullable: false),
                    PorcentajeCobertura = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ObrasSociales", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Turnos_ObraSocialId",
                table: "Turnos",
                column: "ObraSocialId");

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

            migrationBuilder.AddForeignKey(
                name: "FK_Turnos_ObrasSociales_ObraSocialId",
                table: "Turnos",
                column: "ObraSocialId",
                principalTable: "ObrasSociales",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Pacientes_ObrasSociales_ObraSocialId",
                table: "Pacientes");

            migrationBuilder.DropForeignKey(
                name: "FK_Turnos_ObrasSociales_ObraSocialId",
                table: "Turnos");

            migrationBuilder.DropTable(
                name: "ObrasSociales");

            migrationBuilder.DropIndex(
                name: "IX_Turnos_ObraSocialId",
                table: "Turnos");

            migrationBuilder.DropIndex(
                name: "IX_Pacientes_ObraSocialId",
                table: "Pacientes");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Turnos");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "Turnos");

            migrationBuilder.DropColumn(
                name: "NotasFacturacion",
                table: "Turnos");

            migrationBuilder.DropColumn(
                name: "ObraSocialId",
                table: "Turnos");

            migrationBuilder.DropColumn(
                name: "EsMayorDeEdad",
                table: "Pacientes");

            migrationBuilder.DropColumn(
                name: "NumeroAfiliado",
                table: "Pacientes");

            migrationBuilder.DropColumn(
                name: "ObraSocialId",
                table: "Pacientes");

            migrationBuilder.DropColumn(
                name: "ResponsableId",
                table: "Pacientes");

            migrationBuilder.DropColumn(
                name: "TipoPago",
                table: "Pacientes");
        }
    }
}
