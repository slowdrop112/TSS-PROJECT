-- Script pentru crearea tabelului UserProfiles
-- Rulează acest script în Azure Data Studio ÎNAINTE de AddXPColumnToUserProfiles.sql

-- Verifică dacă tabelul UserProfiles există deja
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[UserProfiles]') AND type in (N'U'))
BEGIN
    -- Creează tabelul UserProfiles
    CREATE TABLE [UserProfiles] (
        [UserId] nvarchar(450) NOT NULL,
        [FirstName] nvarchar(100) NOT NULL,
        [LastName] nvarchar(100) NOT NULL,
        [XP] int NOT NULL DEFAULT 0,
        CONSTRAINT [PK_UserProfiles] PRIMARY KEY ([UserId]),
        CONSTRAINT [FK_UserProfiles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
    
    PRINT 'Tabelul UserProfiles a fost creat cu succes.';
    
    -- Creează index pentru XP (opțional, pentru performanță la clasament)
    CREATE INDEX [IX_UserProfiles_XP] ON [UserProfiles] ([XP] DESC);
    PRINT 'Index pentru XP a fost creat.';
END
ELSE
BEGIN
    PRINT 'Tabelul UserProfiles există deja.';
    
    -- Verifică dacă coloana XP există
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[UserProfiles]') AND name = 'XP')
    BEGIN
        ALTER TABLE [UserProfiles]
        ADD [XP] int NOT NULL DEFAULT 0;
        PRINT 'Coloana XP a fost adăugată la tabelul existent.';
        
        -- Adaugă index pentru XP
        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_UserProfiles_XP' AND object_id = OBJECT_ID(N'[dbo].[UserProfiles]'))
        BEGIN
            CREATE INDEX [IX_UserProfiles_XP] ON [UserProfiles] ([XP] DESC);
            PRINT 'Index pentru XP a fost creat.';
        END
    END
    ELSE
    BEGIN
        PRINT 'Coloana XP există deja.';
    END
END

-- Verifică structura finală
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE,
    COLUMN_DEFAULT
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'UserProfiles'
ORDER BY ORDINAL_POSITION;

PRINT 'Script finalizat!';
