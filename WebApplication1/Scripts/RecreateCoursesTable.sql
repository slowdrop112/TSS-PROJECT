-- Script pentru a recreea corect tabelul Courses
-- ATENȚIE: Acest script va șterge toate datele din tabelul Courses!
-- Rulează acest script DOAR dacă ești sigur că vrei să ștergi datele existente

-- Pasul 1: Șterge tabelul CourseEnrollments (dacă există) pentru a putea șterge Courses
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[CourseEnrollments]') AND type in (N'U'))
BEGIN
    DROP TABLE [CourseEnrollments];
    PRINT 'Tabelul CourseEnrollments a fost șters.';
END

-- Pasul 2: Șterge tabelul Courses (dacă există)
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Courses]') AND type in (N'U'))
BEGIN
    DROP TABLE [Courses];
    PRINT 'Tabelul Courses a fost șters.';
END

-- Pasul 3: Creează tabelul Courses cu structura corectă
CREATE TABLE [Courses] (
    [Id] int NOT NULL IDENTITY(1,1),
    [Title] nvarchar(200) NOT NULL,
    [Description] nvarchar(1000) NULL,
    [ProfesorId] nvarchar(450) NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    CONSTRAINT [PK_Courses] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Courses_AspNetUsers_ProfesorId] FOREIGN KEY ([ProfesorId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION
);
PRINT 'Tabelul Courses a fost creat cu structura corectă.';

-- Pasul 4: Creează index pentru ProfesorId
CREATE INDEX [IX_Courses_ProfesorId] ON [Courses] ([ProfesorId]);
PRINT 'Index pentru ProfesorId a fost creat.';

-- Pasul 5: Creează tabelul CourseEnrollments
CREATE TABLE [CourseEnrollments] (
    [Id] int NOT NULL IDENTITY(1,1),
    [CourseId] int NOT NULL,
    [StudentId] nvarchar(450) NOT NULL,
    [EnrollmentDate] datetime2 NOT NULL,
    CONSTRAINT [PK_CourseEnrollments] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_CourseEnrollments_AspNetUsers_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_CourseEnrollments_Courses_CourseId] FOREIGN KEY ([CourseId]) REFERENCES [Courses] ([Id]) ON DELETE CASCADE
);
PRINT 'Tabelul CourseEnrollments a fost creat.';

-- Pasul 6: Creează indexuri pentru CourseEnrollments
CREATE INDEX [IX_CourseEnrollments_StudentId] ON [CourseEnrollments] ([StudentId]);
CREATE UNIQUE INDEX [IX_CourseEnrollments_CourseId_StudentId] ON [CourseEnrollments] ([CourseId], [StudentId]);
PRINT 'Indexuri pentru CourseEnrollments au fost create.';

-- Pasul 7: Verifică structura finală
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    CHARACTER_MAXIMUM_LENGTH,
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Courses'
ORDER BY ORDINAL_POSITION;

PRINT 'Script finalizat cu succes!';





