-- Script pentru crearea tabelei NoteComments
-- Rulează acest script în Azure Data Studio DUPĂ CreateNotesTable.sql

USE [Uniflow] -- Înlocuiește cu numele bazei tale de date
GO

-- Creează tabelul NoteComments dacă nu există
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[NoteComments]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[NoteComments] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [NoteId] INT NOT NULL,
        [AuthorId] NVARCHAR(450) NOT NULL,
        [Content] NVARCHAR(2000) NOT NULL,
        [CreatedDate] DATETIME2 NOT NULL,
        [ParentCommentId] INT NULL,
        CONSTRAINT [PK_NoteComments] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_NoteComments_Notes] FOREIGN KEY ([NoteId]) 
            REFERENCES [dbo].[Notes] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_NoteComments_AspNetUsers] FOREIGN KEY ([AuthorId]) 
            REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_NoteComments_ParentComment] FOREIGN KEY ([ParentCommentId]) 
            REFERENCES [dbo].[NoteComments] ([Id]) ON DELETE NO ACTION
    );
    
    CREATE NONCLUSTERED INDEX [IX_NoteComments_NoteId] ON [dbo].[NoteComments] ([NoteId]);
    CREATE NONCLUSTERED INDEX [IX_NoteComments_AuthorId] ON [dbo].[NoteComments] ([AuthorId]);
    CREATE NONCLUSTERED INDEX [IX_NoteComments_CreatedDate] ON [dbo].[NoteComments] ([CreatedDate]);
    
    PRINT 'Tabelul NoteComments a fost creat cu succes!';
END
ELSE
BEGIN
    PRINT 'Tabelul NoteComments există deja.';
END
GO

