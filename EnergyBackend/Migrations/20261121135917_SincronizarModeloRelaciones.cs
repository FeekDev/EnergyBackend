using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnergyBackend.Migrations
{
    /// <inheritdoc />
    public partial class SincronizarModeloRelaciones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS (
                    SELECT 1
                    FROM sys.columns c
                    INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
                    WHERE c.object_id = OBJECT_ID('dbo.Direcciones')
                      AND c.name = 'idCasa'
                      AND t.name IN ('nvarchar', 'varchar', 'nchar', 'char')
                )
                BEGIN
                    UPDATE dbo.Direcciones
                    SET idCasa = NULL
                    WHERE idCasa IS NOT NULL
                      AND TRY_CONVERT(int, idCasa) IS NULL;

                    ALTER TABLE dbo.Direcciones
                    ALTER COLUMN idCasa int NULL;
                END
                """);

            migrationBuilder.Sql("""
                IF NOT EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE name = 'IX_Direcciones_idCasa'
                      AND object_id = OBJECT_ID('dbo.Direcciones')
                )
                BEGIN
                    CREATE UNIQUE INDEX IX_Direcciones_idCasa
                    ON dbo.Direcciones(idCasa)
                    WHERE idCasa IS NOT NULL;
                END

                IF COL_LENGTH('dbo.Casas', 'UsuarioId') IS NOT NULL
                   AND NOT EXISTS (
                       SELECT 1
                       FROM sys.indexes
                       WHERE name = 'IX_Casas_UsuarioId'
                         AND object_id = OBJECT_ID('dbo.Casas')
                   )
                BEGIN
                    CREATE INDEX IX_Casas_UsuarioId
                    ON dbo.Casas(UsuarioId);
                END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Direcciones_idCasa",
                table: "Direcciones");

            migrationBuilder.Sql("""
                IF EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE name = 'IX_Casas_UsuarioId'
                      AND object_id = OBJECT_ID('dbo.Casas')
                )
                BEGIN
                    DROP INDEX IX_Casas_UsuarioId ON dbo.Casas;
                END
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Direcciones_CasaId",
                table: "Direcciones",
                column: "idCasa",
                unique: true);
        }
    }
}
