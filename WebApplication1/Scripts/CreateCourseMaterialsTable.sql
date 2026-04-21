USE [Uniflow] -- Înlocuiește cu numele bazei tale de date
GO

-- Verifică dacă tabelul CourseMaterials există deja
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[CourseMaterials]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[CourseMaterials] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [FileName] NVARCHAR(255) NOT NULL,
        [FilePath] NVARCHAR(500) NOT NULL,
        [Description] NVARCHAR(500) NULL,
        [CourseId] INT NOT NULL,
        [UploadedByUserId] NVARCHAR(450) NOT NULL,
        [UploadDate] DATETIME2 NOT NULL,
        [FileSize] BIGINT NOT NULL,
        [ContentType] NVARCHAR(100) NULL,
        CONSTRAINT [PK_CourseMaterials] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_CourseMaterials_Courses] FOREIGN KEY ([CourseId])
            REFERENCES [dbo].[Courses] ([CourseID])
            ON DELETE CASCADE,
        CONSTRAINT [FK_CourseMaterials_AspNetUsers] FOREIGN KEY ([UploadedByUserId])
            REFERENCES [dbo].[AspNetUsers] ([Id])
            ON DELETE NO ACTION
    );

    -- Index pentru performanță
    CREATE NONCLUSTERED INDEX [IX_CourseMaterials_CourseId]
        ON [dbo].[CourseMaterials] ([CourseId]);

    CREATE NONCLUSTERED INDEX [IX_CourseMaterials_UploadedByUserId]
        ON [dbo].[CourseMaterials] ([UploadedByUserId]);

    PRINT 'Tabelul CourseMaterials a fost creat cu succes!';
END
ELSE
BEGIN
    PRINT 'Tabelul CourseMaterials există deja.';
END
GO

