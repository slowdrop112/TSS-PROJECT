-- Script pentru a corecta numele coloanelor din tabelul Courses
-- ATENȚIE: Rulează acest script DOAR dacă coloanele au nume greșite!

-- Verifică mai întâi structura cu scriptul CheckAndFixCoursesTable.sql
-- Apoi decomentează și rulează comenzile de mai jos dacă este necesar

-- Corectează 'Profesorld' -> 'ProfesorId' (dacă există)
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Courses') AND name = 'Profesorld')
BEGIN
    EXEC sp_rename 'Courses.Profesorld', 'ProfesorId', 'COLUMN';
    PRINT 'Coloana Profesorld a fost redenumită în ProfesorId.';
END

-- Verifică dacă coloana Id există (ar trebui să existe)
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Courses') AND name = 'Id')
BEGIN
    PRINT 'EROARE: Coloana Id nu există în tabelul Courses!';
    -- Dacă nu există, trebuie să o adaugi manual sau să recreezi tabelul
END

-- Verifică dacă coloana CreatedDate există (ar trebui să existe)
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Courses') AND name = 'CreatedDate')
BEGIN
    PRINT 'EROARE: Coloana CreatedDate nu există în tabelul Courses!';
    -- Dacă nu există, trebuie să o adaugi manual
    ALTER TABLE Courses ADD CreatedDate datetime2 NOT NULL DEFAULT GETDATE();
    PRINT 'Coloana CreatedDate a fost adăugată.';
END

-- Verifică structura finală
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Courses'
ORDER BY ORDINAL_POSITION;





