-- Script manual pentru crearea tabelelor Vouchers și UserVouchers

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Vouchers')
BEGIN
    CREATE TABLE [Vouchers] (
        [Id] int IDENTITY(1,1) NOT NULL,
        [Title] nvarchar(200) NOT NULL,
        [Description] nvarchar(1000) NOT NULL,
        [PartnerName] nvarchar(200) NOT NULL,
        [DiscountType] nvarchar(50) NOT NULL,
        [DiscountValue] nvarchar(50) NOT NULL,
        [RequiredLevel] int NOT NULL,
        [ValidityDays] int NOT NULL,
        [IconUrl] nvarchar(500) NULL,
        [IsActive] bit NOT NULL DEFAULT 1,
        CONSTRAINT [PK_Vouchers] PRIMARY KEY ([Id])
    );
    PRINT 'Table Vouchers created successfully.';
END
ELSE
BEGIN
    PRINT 'Table Vouchers already exists.';
END

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'UserVouchers')
BEGIN
    CREATE TABLE [UserVouchers] (
        [Id] int IDENTITY(1,1) NOT NULL,
        [UserId] nvarchar(450) NOT NULL,
        [VoucherId] int NOT NULL,
        [Code] nvarchar(50) NOT NULL,
        [AwardedDate] datetime2 NOT NULL,
        [ExpiryDate] datetime2 NOT NULL,
        [IsRedeemed] bit NOT NULL DEFAULT 0,
        [RedeemedDate] datetime2 NULL,
        CONSTRAINT [PK_UserVouchers] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_UserVouchers_AspNetUsers_UserId] FOREIGN KEY ([UserId]) 
            REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_UserVouchers_Vouchers_VoucherId] FOREIGN KEY ([VoucherId]) 
            REFERENCES [Vouchers] ([Id]) ON DELETE NO ACTION
    );
    
    CREATE UNIQUE NONCLUSTERED INDEX [IX_UserVouchers_Code] ON [UserVouchers]([Code]);
    CREATE NONCLUSTERED INDEX [IX_UserVouchers_UserId_IsRedeemed] ON [UserVouchers]([UserId], [IsRedeemed]);
    CREATE NONCLUSTERED INDEX [IX_UserVouchers_VoucherId] ON [UserVouchers]([VoucherId]);
    
    PRINT 'Table UserVouchers created successfully.';
END
ELSE
BEGIN
    PRINT 'Table UserVouchers already exists.';
END

-- Marcare migrare ca aplicată
IF NOT EXISTS (SELECT * FROM __EFMigrationsHistory WHERE MigrationId = '20260204092909_AddVoucherSystem')
BEGIN
    INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('20260204092909_AddVoucherSystem', '8.0.1');
    PRINT 'Migration marked as applied in __EFMigrationsHistory.';
END
