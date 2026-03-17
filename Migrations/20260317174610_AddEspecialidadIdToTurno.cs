using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace turnero_medico_backend.Migrations
{
    /// <inheritdoc />
    public partial class AddEspecialidadIdToTurno : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Especialidad",
                table: "Turnos");

            migrationBuilder.AddColumn<int>(
                name: "EspecialidadId",
                table: "Turnos",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Turnos_EspecialidadId",
                table: "Turnos",
                column: "EspecialidadId");

            migrationBuilder.AddForeignKey(
                name: "FK_Turnos_Especialidades_EspecialidadId",
                table: "Turnos",
                column: "EspecialidadId",
                principalTable: "Especialidades",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Turnos_Especialidades_EspecialidadId",
                table: "Turnos");

            migrationBuilder.DropIndex(
                name: "IX_Turnos_EspecialidadId",
                table: "Turnos");

            migrationBuilder.DropColumn(
                name: "EspecialidadId",
                table: "Turnos");

            migrationBuilder.AddColumn<string>(
                name: "Especialidad",
                table: "Turnos",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
