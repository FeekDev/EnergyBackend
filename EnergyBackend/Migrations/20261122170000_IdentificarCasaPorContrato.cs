using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnergyBackend.Migrations
{
    /// <inheritdoc />
    public partial class IdentificarCasaPorContrato : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Casas_UsuarioId",
                table: "Casas");

            migrationBuilder.AddColumn<string>(
                name: "NumeroContrato",
                table: "Casas",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Casas_UsuarioId_NumeroContrato",
                table: "Casas",
                columns: new[] { "UsuarioId", "NumeroContrato" },
                unique: true,
                filter: "[NumeroContrato] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Casas_UsuarioId_NumeroContrato",
                table: "Casas");

            migrationBuilder.DropColumn(
                name: "NumeroContrato",
                table: "Casas");

            migrationBuilder.CreateIndex(
                name: "IX_Casas_UsuarioId",
                table: "Casas",
                column: "UsuarioId");
        }
    }
}
