USE Northwind;
GO

-- Only create if not exists (idempotent)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ShippingGeocodes')
BEGIN
    CREATE TABLE [dbo].[ShippingGeocodes] (
        [Id]           UNIQUEIDENTIFIER NOT NULL,
        [OrderId]      INT              NOT NULL,
        [Street]       NVARCHAR(120)    NOT NULL,
        [City]         NVARCHAR(50)     NOT NULL,
        [Region]       NVARCHAR(50)     NULL,
        [PostalCode]   NVARCHAR(20)     NULL,
        [Country]      NVARCHAR(50)     NOT NULL,
        [Latitude]     FLOAT            NOT NULL,
        [Longitude]    FLOAT            NOT NULL,
        [PlaceType]    NVARCHAR(50)     NOT NULL,
        [RawResponse]  NVARCHAR(MAX)    NOT NULL,
        [ValidatedAt]  DATETIME2        NOT NULL,

        CONSTRAINT [PK_ShippingGeocodes] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ShippingGeocodes_Orders_OrderId] FOREIGN KEY ([OrderId])
            REFERENCES [dbo].[Orders] ([OrderID]) ON DELETE CASCADE
    );

    CREATE UNIQUE INDEX [IX_ShippingGeocodes_OrderId]
        ON [dbo].[ShippingGeocodes] ([OrderId]);

    PRINT 'ShippingGeocodes table created successfully.';
END
ELSE
BEGIN
    PRINT 'ShippingGeocodes table already exists. Skipping.';
END
GO

-- Create EF Core migrations history if not exists
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = '__EFMigrationsHistory')
BEGIN
    CREATE TABLE [dbo].[__EFMigrationsHistory] (
        [MigrationId]    NVARCHAR(150) NOT NULL,
        [ProductVersion] NVARCHAR(32)  NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END
GO

-- Mark migration as applied so EF Core doesn't try to re-apply it
IF NOT EXISTS (SELECT * FROM [dbo].[__EFMigrationsHistory] WHERE [MigrationId] = '20260426033624_InitialCreate')
BEGIN
    INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES ('20260426033624_InitialCreate', '10.0.0');

    PRINT 'Migration marked as applied in __EFMigrationsHistory.';
END
GO