-- Script pentru a adăuga coloana FullName în tabelul AspNetUsers
-- Rulează acest script în Azure Data Studio

-- Verifică dacă coloana există deja
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('AspNetUsers') AND name = 'FullName')
BEGIN
    ALTER TABLE AspNetUsers ADD FullName nvarchar(100) NULL;
    PRINT 'Coloana FullName a fost adăugată în AspNetUsers.';
    
    -- Setează FullName pentru utilizatorii existenți bazat pe email (opțional)
    -- UPDATE AspNetUsers SET FullName = LEFT(Email, CHARINDEX('@', Email) - 1) WHERE FullName IS NULL;
END
ELSE
BEGIN
    PRINT 'Coloana FullName există deja în AspNetUsers.';
END

-- Verifică structura
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    CHARACTER_MAXIMUM_LENGTH,
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'AspNetUsers' AND COLUMN_NAME = 'FullName';

PRINT 'Script finalizat!';

