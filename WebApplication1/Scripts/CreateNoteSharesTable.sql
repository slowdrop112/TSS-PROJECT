-- Script pentru crearea tabelei NoteShares
-- Rulează acest script în Azure Data Studio DUPĂ CreateNotesTable.sql

USE [Uniflow] -- Înlocuiește cu numele bazei tale de date
GO

-- Creează tabelul NoteShares dacă nu există
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[NoteShares]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[NoteShares] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [NoteId] INT NOT NULL,
        [OwnerId] NVARCHAR(450) NOT NULL,
        [SharedWithUserId] NVARCHAR(450) NOT NULL,
        [SharedDate] DATETIME2 NOT NULL,
        CONSTRAINT [PK_NoteShares] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_NoteShares_Notes] FOREIGN KEY ([NoteId]) 
            REFERENCES [dbo].[Notes] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_NoteShares_AspNetUsers_Owner] FOREIGN KEY ([OwnerId]) 
            REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_NoteShares_AspNetUsers_SharedWith] FOREIGN KEY ([SharedWithUserId]) 
            REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE NO ACTION
    );
    
    -- Index unic pentru a preveni partajări duplicate
    CREATE UNIQUE NONCLUSTERED INDEX [IX_NoteShares_NoteId_SharedWithUserId] 
        ON [dbo].[NoteShares] ([NoteId], [SharedWithUserId]);
    
    -- Indexuri pentru performanță
    CREATE NONCLUSTERED INDEX [IX_NoteShares_NoteId] ON [dbo].[NoteShares] ([NoteId]);
    CREATE NONCLUSTERED INDEX [IX_NoteShares_OwnerId] ON [dbo].[NoteShares] ([OwnerId]);
    CREATE NONCLUSTERED INDEX [IX_NoteShares_SharedWithUserId] ON [dbo].[NoteShares] ([SharedWithUserId]);
    
    PRINT 'Tabelul NoteShares a fost creat cu succes!';
END
ELSE
BEGIN
    PRINT 'Tabelul NoteShares există deja.';
END
GO


