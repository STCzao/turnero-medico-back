using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace turnero_medico_backend.Migrations
{
    /// <inheritdoc />
    public partial class Fase2_Horarios_Indices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Horarios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DoctorId = table.Column<int>(type: "integer", nullable: false),
                    DiaSemana = table.Column<int>(type: "integer", nullable: false),
                    HoraInicio = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    HoraFin = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    DuracionMinutos = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Horarios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Horarios_Doctores_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "Doctores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Turnos_Estado",
                table: "Turnos",
                column: "Estado");

            migrationBuilder.CreateIndex(
                name: "IX_Pacientes_ResponsableId",
                table: "Pacientes",
                column: "ResponsableId");

            migrationBuilder.CreateIndex(
                name: "IX_Pacientes_UserId",
                table: "Pacientes",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Doctores_UserId",
                table: "Doctores",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Horarios_DoctorId_DiaSemana",
                table: "Horarios",
                columns: new[] { "DoctorId", "DiaSemana" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Horarios");

            migrationBuilder.DropIndex(
                name: "IX_Turnos_Estado",
                table: "Turnos");

            migrationBuilder.DropIndex(
                name: "IX_Pacientes_ResponsableId",
                table: "Pacientes");

            migrationBuilder.DropIndex(
                name: "IX_Pacientes_UserId",
                table: "Pacientes");

            migrationBuilder.DropIndex(
                name: "IX_Doctores_UserId",
                table: "Doctores");
        }
    }
}
