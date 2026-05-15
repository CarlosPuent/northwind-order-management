USE Northwind;
GO

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'Orders' AND COLUMN_NAME = 'IsDeleted'
)
BEGIN
    ALTER TABLE [Orders] ADD [IsDeleted] BIT NOT NULL DEFAULT 0;
    ALTER TABLE [Orders] ADD [DeletedAt] DATETIME2 NULL;
    CREATE INDEX [IX_Orders_IsDeleted] ON [Orders] ([IsDeleted]);

    IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = '20260514041222_AddSoftDeleteToOrders')
    BEGIN
        INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
        VALUES ('20260514041222_AddSoftDeleteToOrders', '10.0.0');
    END
END