-- Script pentru crearea tabelei NoteVotes
-- Rulează acest script în Azure Data Studio DUPĂ CreateNotesTable.sql

USE [Uniflow] -- Înlocuiește cu numele bazei tale de date
GO

-- Creează tabelul NoteVotes dacă nu există
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[NoteVotes]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[NoteVotes] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [NoteId] INT NOT NULL,
        [UserId] NVARCHAR(450) NOT NULL,
        [IsUpvote] BIT NOT NULL,
        [VoteDate] DATETIME2 NOT NULL,
        CONSTRAINT [PK_NoteVotes] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_NoteVotes_Notes] FOREIGN KEY ([NoteId]) 
            REFERENCES [dbo].[Notes] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_NoteVotes_AspNetUsers] FOREIGN KEY ([UserId]) 
            REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE NO ACTION
    );
    
    -- Index unic pentru a preveni voturi duplicate (un user poate vota o dată per notiță)
    CREATE UNIQUE NONCLUSTERED INDEX [IX_NoteVotes_NoteId_UserId] 
        ON [dbo].[NoteVotes] ([NoteId], [UserId]);
    
    -- Indexuri pentru performanță
    CREATE NONCLUSTERED INDEX [IX_NoteVotes_NoteId] ON [dbo].[NoteVotes] ([NoteId]);
    CREATE NONCLUSTERED INDEX [IX_NoteVotes_UserId] ON [dbo].[NoteVotes] ([UserId]);
    
    PRINT 'Tabelul NoteVotes a fost creat cu succes!';
END
ELSE
BEGIN
    PRINT 'Tabelul NoteVotes există deja.';
END
GO


