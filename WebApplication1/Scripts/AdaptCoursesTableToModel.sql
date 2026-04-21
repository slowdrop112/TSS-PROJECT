-- Script pentru a adapta tabelul Courses existent la modelul aplicației
-- Acest script păstrează datele existente și adaugă/modifică doar ce este necesar

-- Pasul 1: Redenumește CourseID în Id (dacă nu există deja coloana Id)
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Courses') AND name = 'CourseID')
BEGIN
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Courses') AND name = 'Id')
    BEGIN
        EXEC sp_rename 'Courses.CourseID', 'Id', 'COLUMN';
        PRINT 'Coloana CourseID a fost redenumită în Id.';
    END
    ELSE
    BEGIN
        -- Dacă există deja Id, șterge CourseID
        ALTER TABLE Courses DROP COLUMN CourseID;
        PRINT 'Coloana CourseID a fost ștearsă (Id există deja).';
    END
END

-- Pasul 2: Redenumește DateCreated în CreatedDate (dacă nu există deja)
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Courses') AND name = 'DateCreated')
BEGIN
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Courses') AND name = 'CreatedDate')
    BEGIN
        EXEC sp_rename 'Courses.DateCreated', 'CreatedDate', 'COLUMN';
        PRINT 'Coloana DateCreated a fost redenumită în CreatedDate.';
    END
    ELSE
    BEGIN
        -- Dacă există deja CreatedDate, șterge DateCreated
        ALTER TABLE Courses DROP COLUMN DateCreated;
        PRINT 'Coloana DateCreated a fost ștearsă (CreatedDate există deja).';
    END
END

-- Pasul 3: Adaugă coloana ProfesorId dacă nu există (CRITIC!)
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Courses') AND name = 'ProfesorId')
BEGIN
    -- Adaugă coloana ProfesorId (va fi nullable temporar pentru a permite adăugarea)
    ALTER TABLE Courses ADD ProfesorId nvarchar(450) NULL;
    PRINT 'Coloana ProfesorId a fost adăugată (temporar nullable).';
    
    -- Setează un ProfesorId default pentru înregistrările existente (folosește primul admin)
    DECLARE @DefaultProfesorId nvarchar(450);
    SELECT TOP 1 @DefaultProfesorId = Id FROM AspNetUsers WHERE Id IN (
        SELECT UserId FROM AspNetUserRoles WHERE RoleId IN (
            SELECT Id FROM AspNetRoles WHERE Name = 'Admin'
        )
    );
    
    IF @DefaultProfesorId IS NOT NULL
    BEGIN
        UPDATE Courses SET ProfesorId = @DefaultProfesorId WHERE ProfesorId IS NULL;
        PRINT 'ProfesorId a fost setat pentru înregistrările existente.';
    END
    ELSE
    BEGIN
        -- Dacă nu există admin, folosește primul user
        SELECT TOP 1 @DefaultProfesorId = Id FROM AspNetUsers;
        IF @DefaultProfesorId IS NOT NULL
        BEGIN
            UPDATE Courses SET ProfesorId = @DefaultProfesorId WHERE ProfesorId IS NULL;
            PRINT 'ProfesorId a fost setat folosind primul utilizator disponibil.';
        END
    END
    
    -- Face coloana NOT NULL acum că are valori
    ALTER TABLE Courses ALTER COLUMN ProfesorId nvarchar(450) NOT NULL;
    PRINT 'Coloana ProfesorId este acum NOT NULL.';
    
    -- Adaugă foreign key constraint
    ALTER TABLE Courses
    ADD CONSTRAINT FK_Courses_AspNetUsers_ProfesorId 
    FOREIGN KEY (ProfesorId) REFERENCES AspNetUsers(Id) ON DELETE NO ACTION;
    PRINT 'Foreign key constraint pentru ProfesorId a fost adăugat.';
    
    -- Adaugă index pentru ProfesorId
    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Courses_ProfesorId' AND object_id = OBJECT_ID('Courses'))
    BEGIN
        CREATE INDEX IX_Courses_ProfesorId ON Courses(ProfesorId);
        PRINT 'Index pentru ProfesorId a fost creat.';
    END
END
ELSE
BEGIN
    PRINT 'Coloana ProfesorId există deja.';
END

-- Pasul 4: Verifică dacă Id este IDENTITY (auto-increment)
IF NOT EXISTS (
    SELECT * FROM sys.columns 
    WHERE object_id = OBJECT_ID('Courses') 
    AND name = 'Id' 
    AND is_identity = 1
)
BEGIN
    -- Dacă nu este IDENTITY, trebuie să o facem (mai complex, dar necesar)
    PRINT 'ATENȚIE: Coloana Id nu este IDENTITY. Poate fi necesar să recreezi tabelul.';
END

-- Pasul 5: Verifică structura finală
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    CHARACTER_MAXIMUM_LENGTH,
    IS_NULLABLE,
    COLUMNPROPERTY(OBJECT_ID('Courses'), COLUMN_NAME, 'IsIdentity') AS IS_IDENTITY
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Courses'
ORDER BY ORDINAL_POSITION;

PRINT 'Script finalizat! Verifică structura de mai sus.';





