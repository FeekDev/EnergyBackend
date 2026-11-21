IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
CREATE TABLE [Casas] (
    [Id] int NOT NULL IDENTITY,
    [CantidadPersonas] nvarchar(max) NOT NULL,
    [IsComplete] bit NOT NULL,
    CONSTRAINT [PK_Casas] PRIMARY KEY ([Id])
);

CREATE TABLE [Direcciones] (
    [Id] int NOT NULL IDENTITY,
    [Calle] nvarchar(max) NOT NULL,
    [Ciudad] nvarchar(max) NOT NULL,
    [Estado] nvarchar(max) NOT NULL,
    [CodigoPostal] nvarchar(max) NOT NULL,
    [idCasa] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_Direcciones] PRIMARY KEY ([Id])
);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261120025038_inicial', N'10.0.11');

COMMIT;
GO

BEGIN TRANSACTION;
CREATE TABLE [AhorrosEnergia] (
    [Id] int NOT NULL IDENTITY,
    [TipoAhorro] nvarchar(max) NOT NULL,
    [Descripcion] nvarchar(max) NOT NULL,
    [idCasa] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_AhorrosEnergia] PRIMARY KEY ([Id])
);

CREATE TABLE [Personas] (
    [Id] int NOT NULL IDENTITY,
    [Nombre] nvarchar(max) NOT NULL,
    [Apellido] nvarchar(max) NOT NULL,
    [CorreoElectronico] nvarchar(max) NOT NULL,
    [Telefono] nvarchar(max) NOT NULL,
    [idCasa] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_Personas] PRIMARY KEY ([Id])
);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261120031038_inicialv2', N'10.0.11');

COMMIT;
GO

BEGIN TRANSACTION;
CREATE TABLE [Usuarios] (
    [Id] int NOT NULL IDENTITY,
    [Nombre] nvarchar(max) NOT NULL,
    [CorreoElectronico] nvarchar(450) NOT NULL,
    [PasswordHash] nvarchar(max) NOT NULL,
    [PasswordSalt] nvarchar(max) NOT NULL,
    [CreadoEn] datetime2 NOT NULL,
    CONSTRAINT [PK_Usuarios] PRIMARY KEY ([Id])
);

CREATE UNIQUE INDEX [IX_Usuarios_CorreoElectronico] ON [Usuarios] ([CorreoElectronico]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261121123446_AgregarUsuarios', N'10.0.11');

COMMIT;
GO

BEGIN TRANSACTION;
INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261121131540_bd', N'10.0.11');

COMMIT;
GO

BEGIN TRANSACTION;
CREATE TABLE [Facturas] (
    [Id] int NOT NULL IDENTITY,
    [UsuarioId] int NOT NULL,
    [NumeroContrato] nvarchar(100) NOT NULL,
    [FechaEmision] datetime2 NOT NULL,
    [FechaPago] datetime2 NULL,
    [ValorAcueducto] decimal(18,2) NOT NULL,
    [ValorAlcantarillado] decimal(18,2) NOT NULL,
    [ValorEnergia] decimal(18,2) NOT NULL,
    [ValorGas] decimal(18,2) NOT NULL,
    [CreadoEn] datetime2 NOT NULL,
    CONSTRAINT [PK_Facturas] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Facturas_Usuarios_UsuarioId] FOREIGN KEY ([UsuarioId]) REFERENCES [Usuarios] ([Id]) ON DELETE CASCADE
);

CREATE INDEX [IX_Facturas_UsuarioId] ON [Facturas] ([UsuarioId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261121131841_AgregarFacturas', N'10.0.11');

COMMIT;
GO

BEGIN TRANSACTION;
DROP INDEX [IX_Direcciones_CasaId] ON [Direcciones];

CREATE UNIQUE INDEX [IX_Direcciones_CasaId] ON [Direcciones] ([CasaId]) WHERE [CasaId] IS NOT NULL;

IF COL_LENGTH(N'dbo.Casas', N'UsuarioId') IS NOT NULL
   AND NOT EXISTS (
       SELECT 1
       FROM sys.indexes
       WHERE name = N'IX_Casas_UsuarioId'
         AND object_id = OBJECT_ID(N'dbo.Casas')
   )
BEGIN
    CREATE INDEX [IX_Casas_UsuarioId] ON [dbo].[Casas] ([UsuarioId]);
END;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261121135917_SincronizarModeloRelaciones', N'10.0.11');

COMMIT;
GO

BEGIN TRANSACTION;
DROP INDEX [IX_Direcciones_idCasa] ON [Direcciones];
CREATE UNIQUE INDEX [IX_Direcciones_idCasa]
    ON [Direcciones] ([idCasa]) WHERE [idCasa] IS NOT NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261121142749_AjustarRelacionDireccionCasa', N'10.0.11');

COMMIT;
GO

BEGIN TRANSACTION;
DROP TABLE [AhorrosEnergia];

ALTER TABLE [Casas] ADD [UsuarioId] int NULL;
ALTER TABLE [Facturas] ADD [idCasa] int NULL;

CREATE INDEX [IX_Casas_UsuarioId] ON [Casas] ([UsuarioId]);
CREATE INDEX [IX_Facturas_CasaId] ON [Facturas] ([idCasa]);

ALTER TABLE [Casas] ADD CONSTRAINT [FK_Casas_Usuarios_UsuarioId]
    FOREIGN KEY ([UsuarioId]) REFERENCES [Usuarios] ([Id]) ON DELETE CASCADE;
ALTER TABLE [Direcciones] ADD CONSTRAINT [FK_Direcciones_Casas_CasaId]
    FOREIGN KEY ([idCasa]) REFERENCES [Casas] ([Id]) ON DELETE CASCADE;
ALTER TABLE [Facturas] ADD CONSTRAINT [FK_Facturas_Casas_CasaId]
    FOREIGN KEY ([idCasa]) REFERENCES [Casas] ([Id]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261122150000_RelacionarModelos', N'10.0.11');

COMMIT;
GO

BEGIN TRANSACTION;
DELETE FROM [Facturas];

ALTER TABLE [Facturas] DROP COLUMN [ValorAcueducto];
ALTER TABLE [Facturas] DROP COLUMN [ValorAlcantarillado];
ALTER TABLE [Facturas] DROP COLUMN [ValorEnergia];
ALTER TABLE [Facturas] DROP COLUMN [ValorGas];

ALTER TABLE [Facturas] ADD [ConsumoAcueductoM3] decimal(18,3) NOT NULL DEFAULT 0;
ALTER TABLE [Facturas] ADD [ConsumoAlcantarilladoM3] decimal(18,3) NOT NULL DEFAULT 0;
ALTER TABLE [Facturas] ADD [ConsumoEnergiaKwh] decimal(18,3) NOT NULL DEFAULT 0;
ALTER TABLE [Facturas] ADD [ConsumoGasM3] decimal(18,3) NOT NULL DEFAULT 0;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261122160000_RegistrarConsumosServicios', N'10.0.11');

COMMIT;
GO

BEGIN TRANSACTION;
DROP INDEX [IX_Casas_UsuarioId] ON [Casas];

ALTER TABLE [Casas] ADD [NumeroContrato] nvarchar(100) NULL;

CREATE UNIQUE INDEX [IX_Casas_UsuarioId_NumeroContrato]
    ON [Casas] ([UsuarioId], [NumeroContrato])
    WHERE [NumeroContrato] IS NOT NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261122170000_IdentificarCasaPorContrato', N'10.0.11');

COMMIT;
GO
