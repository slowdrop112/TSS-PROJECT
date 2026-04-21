-- Script pentru a adăuga coloanele FirstName și LastName în tabelul AspNetUsers
-- Rulează acest script în Azure Data Studio

-- Verifică dacă coloana FirstName există deja
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('AspNetUsers') AND name = 'FirstName')
BEGIN
    ALTER TABLE AspNetUsers ADD FirstName nvarchar(100) NULL;
    PRINT 'Coloana FirstName a fost adăugată.';
END
ELSE
BEGIN
    PRINT 'Coloana FirstName există deja.';
END

-- Verifică dacă coloana LastName există deja
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('AspNetUsers') AND name = 'LastName')
BEGIN
    ALTER TABLE AspNetUsers ADD LastName nvarchar(100) NULL;
    PRINT 'Coloana LastName a fost adăugată.';
END
ELSE
BEGIN
    PRINT 'Coloana LastName există deja.';
END

-- Verifică structura finală
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    CHARACTER_MAXIMUM_LENGTH,
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'AspNetUsers' 
AND COLUMN_NAME IN ('FirstName', 'LastName')
ORDER BY ORDINAL_POSITION;

PRINT 'Script finalizat!';

