USE [Uniflow] -- Înlocuiește cu numele bazei tale de date
GO

-- Verifică dacă tabelul CourseAnnouncements există deja
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[CourseAnnouncements]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[CourseAnnouncements] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [Title] NVARCHAR(500) NOT NULL,
        [Content] NVARCHAR(MAX) NOT NULL,
        [CourseId] INT NOT NULL,
        [PostedByUserId] NVARCHAR(450) NOT NULL,
        [PostedDate] DATETIME2 NOT NULL,
        [IsImportant] BIT NOT NULL DEFAULT 0,
        CONSTRAINT [PK_CourseAnnouncements] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_CourseAnnouncements_Courses] FOREIGN KEY ([CourseId])
            REFERENCES [dbo].[Courses] ([CourseID])
            ON DELETE CASCADE,
        CONSTRAINT [FK_CourseAnnouncements_AspNetUsers] FOREIGN KEY ([PostedByUserId])
            REFERENCES [dbo].[AspNetUsers] ([Id])
            ON DELETE NO ACTION
    );

    -- Index pentru performanță
    CREATE NONCLUSTERED INDEX [IX_CourseAnnouncements_CourseId]
        ON [dbo].[CourseAnnouncements] ([CourseId]);

    CREATE NONCLUSTERED INDEX [IX_CourseAnnouncements_PostedByUserId]
        ON [dbo].[CourseAnnouncements] ([PostedByUserId]);

    PRINT 'Tabelul CourseAnnouncements a fost creat cu succes!';
END
ELSE
BEGIN
    PRINT 'Tabelul CourseAnnouncements există deja.';
END
GO

