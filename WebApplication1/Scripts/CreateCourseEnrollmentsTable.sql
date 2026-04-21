-- Script pentru a crea tabelul CourseEnrollments
-- Rulează acest script în Azure Data Studio

-- Verifică dacă tabelul există deja
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[CourseEnrollments]') AND type in (N'U'))
BEGIN
    -- Creează tabelul CourseEnrollments
    CREATE TABLE [CourseEnrollments] (
        [Id] int NOT NULL IDENTITY(1,1),
        [CourseId] int NOT NULL,
        [StudentId] nvarchar(450) NOT NULL,
        [EnrollmentDate] datetime2 NOT NULL,
        CONSTRAINT [PK_CourseEnrollments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CourseEnrollments_AspNetUsers_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_CourseEnrollments_Courses_CourseId] FOREIGN KEY ([CourseId]) REFERENCES [Courses] ([CourseID]) ON DELETE CASCADE
    );
    PRINT 'Tabelul CourseEnrollments a fost creat.';
    
    -- Creează indexuri
    CREATE INDEX [IX_CourseEnrollments_StudentId] ON [CourseEnrollments] ([StudentId]);
    CREATE UNIQUE INDEX [IX_CourseEnrollments_CourseId_StudentId] ON [CourseEnrollments] ([CourseId], [StudentId]);
    PRINT 'Indexuri pentru CourseEnrollments au fost create.';
END
ELSE
BEGIN
    PRINT 'Tabelul CourseEnrollments există deja.';
END

-- Verifică structura finală
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    CHARACTER_MAXIMUM_LENGTH,
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'CourseEnrollments'
ORDER BY ORDINAL_POSITION;

PRINT 'Script finalizat!';





