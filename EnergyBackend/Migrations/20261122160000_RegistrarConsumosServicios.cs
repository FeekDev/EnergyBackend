using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnergyBackend.Migrations
{
    /// <inheritdoc />
    public partial class RegistrarConsumosServicios : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM [Facturas];");

            migrationBuilder.DropColumn(
                name: "ValorAcueducto",
                table: "Facturas");

            migrationBuilder.DropColumn(
                name: "ValorAlcantarillado",
                table: "Facturas");

            migrationBuilder.DropColumn(
                name: "ValorEnergia",
                table: "Facturas");

            migrationBuilder.DropColumn(
                name: "ValorGas",
                table: "Facturas");

            migrationBuilder.AddColumn<decimal>(
                name: "ConsumoAcueductoM3",
                table: "Facturas",
                type: "decimal(18,3)",
                precision: 18,
                scale: 3,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ConsumoAlcantarilladoM3",
                table: "Facturas",
                type: "decimal(18,3)",
                precision: 18,
                scale: 3,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ConsumoEnergiaKwh",
                table: "Facturas",
                type: "decimal(18,3)",
                precision: 18,
                scale: 3,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ConsumoGasM3",
                table: "Facturas",
                type: "decimal(18,3)",
                precision: 18,
                scale: 3,
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConsumoAcueductoM3",
                table: "Facturas");

            migrationBuilder.DropColumn(
                name: "ConsumoAlcantarilladoM3",
                table: "Facturas");

            migrationBuilder.DropColumn(
                name: "ConsumoEnergiaKwh",
                table: "Facturas");

            migrationBuilder.DropColumn(
                name: "ConsumoGasM3",
                table: "Facturas");

            migrationBuilder.AddColumn<decimal>(
                name: "ValorAcueducto",
                table: "Facturas",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ValorAlcantarillado",
                table: "Facturas",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ValorEnergia",
                table: "Facturas",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ValorGas",
                table: "Facturas",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);
        }
    }
}
