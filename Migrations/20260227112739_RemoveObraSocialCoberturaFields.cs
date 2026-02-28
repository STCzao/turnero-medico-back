using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace turnero_medico_backend.Migrations
{
    /// <inheritdoc />
    public partial class RemoveObraSocialCoberturaFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Cobertura",
                table: "ObrasSociales");

            migrationBuilder.DropColumn(
                name: "PorcentajeCobertura",
                table: "ObrasSociales");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Cobertura",
                table: "ObrasSociales",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "PorcentajeCobertura",
                table: "ObrasSociales",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);
        }
    }
}
