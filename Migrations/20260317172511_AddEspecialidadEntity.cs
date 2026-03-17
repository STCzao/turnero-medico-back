using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace turnero_medico_backend.Migrations
{
    /// <inheritdoc />
    public partial class AddEspecialidadEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Especialidades",
                table: "ObrasSociales");

            migrationBuilder.DropColumn(
                name: "Especialidad",
                table: "Doctores");

            migrationBuilder.AddColumn<int>(
                name: "EspecialidadId",
                table: "Doctores",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Especialidades",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Especialidades", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ObraSocialEspecialidad",
                columns: table => new
                {
                    EspecialidadesId = table.Column<int>(type: "integer", nullable: false),
                    ObrasSocialesId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ObraSocialEspecialidad", x => new { x.EspecialidadesId, x.ObrasSocialesId });
                    table.ForeignKey(
                        name: "FK_ObraSocialEspecialidad_Especialidades_EspecialidadesId",
                        column: x => x.EspecialidadesId,
                        principalTable: "Especialidades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ObraSocialEspecialidad_ObrasSociales_ObrasSocialesId",
                        column: x => x.ObrasSocialesId,
                        principalTable: "ObrasSociales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Doctores_EspecialidadId",
                table: "Doctores",
                column: "EspecialidadId");

            migrationBuilder.CreateIndex(
                name: "IX_ObraSocialEspecialidad_ObrasSocialesId",
                table: "ObraSocialEspecialidad",
                column: "ObrasSocialesId");

            migrationBuilder.AddForeignKey(
                name: "FK_Doctores_Especialidades_EspecialidadId",
                table: "Doctores",
                column: "EspecialidadId",
                principalTable: "Especialidades",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Doctores_Especialidades_EspecialidadId",
                table: "Doctores");

            migrationBuilder.DropTable(
                name: "ObraSocialEspecialidad");

            migrationBuilder.DropTable(
                name: "Especialidades");

            migrationBuilder.DropIndex(
                name: "IX_Doctores_EspecialidadId",
                table: "Doctores");

            migrationBuilder.DropColumn(
                name: "EspecialidadId",
                table: "Doctores");

            migrationBuilder.AddColumn<List<string>>(
                name: "Especialidades",
                table: "ObrasSociales",
                type: "jsonb",
                nullable: false);

            migrationBuilder.AddColumn<string>(
                name: "Especialidad",
                table: "Doctores",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
