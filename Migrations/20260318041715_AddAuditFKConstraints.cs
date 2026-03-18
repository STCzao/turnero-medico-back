using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace turnero_medico_backend.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditFKConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Rol",
                table: "AspNetUsers");

            migrationBuilder.CreateIndex(
                name: "IX_Turnos_ConfirmadaPorId",
                table: "Turnos",
                column: "ConfirmadaPorId");

            migrationBuilder.CreateIndex(
                name: "IX_Turnos_CreatedByUserId",
                table: "Turnos",
                column: "CreatedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Pacientes_AspNetUsers_ResponsableId",
                table: "Pacientes",
                column: "ResponsableId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Turnos_AspNetUsers_ConfirmadaPorId",
                table: "Turnos",
                column: "ConfirmadaPorId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Turnos_AspNetUsers_CreatedByUserId",
                table: "Turnos",
                column: "CreatedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Pacientes_AspNetUsers_ResponsableId",
                table: "Pacientes");

            migrationBuilder.DropForeignKey(
                name: "FK_Turnos_AspNetUsers_ConfirmadaPorId",
                table: "Turnos");

            migrationBuilder.DropForeignKey(
                name: "FK_Turnos_AspNetUsers_CreatedByUserId",
                table: "Turnos");

            migrationBuilder.DropIndex(
                name: "IX_Turnos_ConfirmadaPorId",
                table: "Turnos");

            migrationBuilder.DropIndex(
                name: "IX_Turnos_CreatedByUserId",
                table: "Turnos");

            migrationBuilder.AddColumn<string>(
                name: "Rol",
                table: "AspNetUsers",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
