-- Script pentru crearea tabelei Notes
-- Rulează acest script în Azure Data Studio

USE [Uniflow] -- Înlocuiește cu numele bazei tale de date
GO

-- Creează tabelul Notes dacă nu există
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Notes]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Notes] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [Title] NVARCHAR(255) NOT NULL,
        [Content] NVARCHAR(MAX) NULL,
        [CourseId] INT NOT NULL,
        [StudentId] NVARCHAR(450) NOT NULL,
        [CreatedDate] DATETIME2 NOT NULL,
        [Status] NVARCHAR(20) NOT NULL DEFAULT 'Pending',
        [ValidatedByUserId] NVARCHAR(450) NULL,
        [ValidationDate] DATETIME2 NULL,
        CONSTRAINT [PK_Notes] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_Notes_Courses] FOREIGN KEY ([CourseId]) 
            REFERENCES [dbo].[Courses] ([CourseID]) ON DELETE CASCADE,
        CONSTRAINT [FK_Notes_AspNetUsers_Student] FOREIGN KEY ([StudentId]) 
            REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_Notes_AspNetUsers_Validator] FOREIGN KEY ([ValidatedByUserId]) 
            REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE NO ACTION
    );
    
    -- Creează indexuri pentru performanță
    CREATE NONCLUSTERED INDEX [IX_Notes_CourseId] ON [dbo].[Notes] ([CourseId]);
    CREATE NONCLUSTERED INDEX [IX_Notes_StudentId] ON [dbo].[Notes] ([StudentId]);
    CREATE NONCLUSTERED INDEX [IX_Notes_Status] ON [dbo].[Notes] ([Status]);
    CREATE NONCLUSTERED INDEX [IX_Notes_CreatedDate] ON [dbo].[Notes] ([CreatedDate] DESC);
    
    PRINT 'Tabelul Notes a fost creat cu succes!';
END
ELSE
BEGIN
    PRINT 'Tabelul Notes există deja.';
END
GO


