using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace turnero_medico_backend.Migrations
{
    /// <inheritdoc />
    public partial class AddObraSocialEspecialidadModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "FechaValidacion",
                table: "Turnos",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MotivoRechazo",
                table: "Turnos",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ValidadoPorDoctorId",
                table: "Turnos",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ObrasSocialesEspecialidades",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ObraSocialId = table.Column<int>(type: "integer", nullable: false),
                    Especialidad = table.Column<string>(type: "text", nullable: false),
                    RequiereValidacionExterna = table.Column<bool>(type: "boolean", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ObrasSocialesEspecialidades", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ObrasSocialesEspecialidades_ObrasSociales_ObraSocialId",
                        column: x => x.ObraSocialId,
                        principalTable: "ObrasSociales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ObrasSocialesEspecialidades_ObraSocialId_Especialidad",
                table: "ObrasSocialesEspecialidades",
                columns: new[] { "ObraSocialId", "Especialidad" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ObrasSocialesEspecialidades");

            migrationBuilder.DropColumn(
                name: "FechaValidacion",
                table: "Turnos");

            migrationBuilder.DropColumn(
                name: "MotivoRechazo",
                table: "Turnos");

            migrationBuilder.DropColumn(
                name: "ValidadoPorDoctorId",
                table: "Turnos");
        }
    }
}
