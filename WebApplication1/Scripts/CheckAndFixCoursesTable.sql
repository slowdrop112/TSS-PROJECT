-- Script pentru a verifica și corecta structura tabelei Courses
-- Rulează acest script în Azure Data Studio pentru a vedea structura actuală

-- Verifică structura tabelei Courses
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    CHARACTER_MAXIMUM_LENGTH,
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Courses'
ORDER BY ORDINAL_POSITION;

-- Dacă vezi că coloanele au nume greșite (ex: 'Profesorld' în loc de 'ProfesorId'),
-- rulează următoarele comenzi pentru a le corecta:

-- Exemplu de corecție (comentează/decomentează după ce vezi structura reală):
/*
-- Dacă coloana se numește 'Profesorld' în loc de 'ProfesorId'
EXEC sp_rename 'Courses.Profesorld', 'ProfesorId', 'COLUMN';

-- Verifică dacă există alte coloane cu nume greșite și corectează-le similar
*/





