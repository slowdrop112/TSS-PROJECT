-- Script pentru adăugarea coloanei XP la tabelul UserProfiles
-- Rulează acest script în Azure Data Studio

-- Verifică dacă tabelul UserProfiles există
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[UserProfiles]') AND type in (N'U'))
BEGIN
    -- Verifică dacă coloana XP există deja
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[UserProfiles]') AND name = 'XP')
    BEGIN
        -- Adaugă coloana XP
        ALTER TABLE [UserProfiles]
        ADD [XP] int NOT NULL DEFAULT 0;
        
        PRINT 'Coloana XP a fost adăugată cu succes la tabelul UserProfiles.';
    END
    ELSE
    BEGIN
        PRINT 'Coloana XP există deja în tabelul UserProfiles.';
    END
END
ELSE
BEGIN
    PRINT 'Tabelul UserProfiles nu există. Creează mai întâi tabelul UserProfiles.';
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




