-- Script pentru marcarea migrărilor vechi ca aplicate și aplicarea doar a tabelelor de voucher
-- ACEST SCRIPT NU MODIFICĂ NIMIC DIN FUNCȚIONALITATEA EXISTENTĂ

-- Pasul 1: Marchează migrările vechi ca aplicate (dacă nu sunt deja)
IF NOT EXISTS (SELECT * FROM __EFMigrationsHistory WHERE MigrationId = '20251213131351_AddRoleRequests')
BEGIN
    INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('20251213131351_AddRoleRequests', '8.0.1');
    PRINT 'Marked migration 20251213131351_AddRoleRequests as applied.';
END

IF NOT EXISTS (SELECT * FROM __EFMigrationsHistory WHERE MigrationId = '20251214094249_AddGamificationSystem')
BEGIN
    INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('20251214094249_AddGamificationSystem', '8.0.1');
    PRINT 'Marked migration 20251214094249_AddGamificationSystem as applied.';
END

IF NOT EXISTS (SELECT * FROM __EFMigrationsHistory WHERE MigrationId = '20260120130407_AddProfesorIdToCourses')
BEGIN
    INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('20260120130407_AddProfesorIdToCourses', '8.0.1');
    PRINT 'Marked migration 20260120130407_AddProfesorIdToCourses as applied.';
END

IF NOT EXISTS (SELECT * FROM __EFMigrationsHistory WHERE MigrationId = '20260129160616_AddBadgesClean')
BEGIN
    INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('20260129160616_AddBadgesClean', '8.0.1');
    PRINT 'Marked migration 20260129160616_AddBadgesClean as applied.';
END

-- Pasul 2: Creează tabelele de voucher DOAR dacă nu există
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
    PRINT 'Table Vouchers already exists - skipping creation.';
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
    PRINT 'Table UserVouchers already exists - skipping creation.';
END

-- Pasul 3: Marchează migrarea de voucher ca aplicată
IF NOT EXISTS (SELECT * FROM __EFMigrationsHistory WHERE MigrationId = '20260204092909_AddVoucherSystem')
BEGIN
    INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('20260204092909_AddVoucherSystem', '8.0.1');
    PRINT 'Migration 20260204092909_AddVoucherSystem marked as applied.';
END

PRINT 'All done! Voucher system is ready.';
