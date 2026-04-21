-- Script pentru verificarea coloanei XP
-- Rulează acest script în Azure Data Studio după ce ai adăugat coloana

-- Verifică dacă coloana XP există
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE,
    COLUMN_DEFAULT
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'UserProfiles'
ORDER BY ORDINAL_POSITION;

-- Verifică XP-ul utilizatorilor
SELECT 
    up.UserId,
    u.Email,
    up.XP
FROM UserProfiles up
INNER JOIN AspNetUsers u ON up.UserId = u.Id
ORDER BY up.XP DESC;




