using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnergyBackend.Migrations
{
    /// <inheritdoc />
    public partial class AjustarRelacionDireccionCasa : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Direcciones_idCasa",
                table: "Direcciones");

            migrationBuilder.CreateIndex(
                name: "IX_Direcciones_idCasa",
                table: "Direcciones",
                column: "idCasa",
                unique: true,
                filter: "[idCasa] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Direcciones_idCasa",
                table: "Direcciones");

            migrationBuilder.CreateIndex(
                name: "IX_Direcciones_idCasa",
                table: "Direcciones",
                column: "idCasa",
                unique: true,
                filter: "[CasaId] IS NOT NULL");
        }
    }
}
