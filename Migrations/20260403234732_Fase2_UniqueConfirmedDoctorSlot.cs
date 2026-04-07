using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace turnero_medico_backend.Migrations
{
    /// <inheritdoc />
    public partial class Fase2_UniqueConfirmedDoctorSlot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Turnos_DoctorId_FechaHora",
                table: "Turnos",
                columns: new[] { "DoctorId", "FechaHora" },
                unique: true,
                filter: "\"Estado\" = 'Confirmado' AND \"DoctorId\" IS NOT NULL AND \"FechaHora\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Turnos_DoctorId_FechaHora",
                table: "Turnos");
        }
    }
}
