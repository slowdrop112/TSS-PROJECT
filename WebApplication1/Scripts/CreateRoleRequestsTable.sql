-- Script pentru a crea tabelul RoleRequests
-- Rulează acest script în Azure Data Studio

-- Verifică dacă tabelul există deja
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[RoleRequests]') AND type in (N'U'))
BEGIN
    -- Creează tabelul RoleRequests
    CREATE TABLE [RoleRequests] (
        [Id] int NOT NULL IDENTITY(1,1),
        [UserId] nvarchar(450) NOT NULL,
        [RequestedRole] nvarchar(50) NOT NULL,
        [Status] nvarchar(20) NOT NULL,
        [RequestDate] datetime2 NOT NULL,
        [ProcessedDate] datetime2 NULL,
        [ProcessedByUserId] nvarchar(450) NULL,
        CONSTRAINT [PK_RoleRequests] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RoleRequests_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_RoleRequests_AspNetUsers_ProcessedByUserId] FOREIGN KEY ([ProcessedByUserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION
    );
    PRINT 'Tabelul RoleRequests a fost creat.';
    
    -- Creează indexuri
    CREATE INDEX [IX_RoleRequests_UserId] ON [RoleRequests] ([UserId]);
    CREATE INDEX [IX_RoleRequests_Status] ON [RoleRequests] ([Status]);
    PRINT 'Indexuri pentru RoleRequests au fost create.';
END
ELSE
BEGIN
    PRINT 'Tabelul RoleRequests există deja.';
END

-- Verifică structura finală
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    CHARACTER_MAXIMUM_LENGTH,
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'RoleRequests'
ORDER BY ORDINAL_POSITION;

PRINT 'Script finalizat!';





