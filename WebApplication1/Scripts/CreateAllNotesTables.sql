-- Script COMPLET pentru crearea tuturor tabelelor pentru sistemul de notițe
-- Rulează acest script în Azure Data Studio
-- Ordinea este importantă din cauza dependențelor de chei străine

USE [Uniflow] -- Înlocuiește cu numele bazei tale de date
GO

PRINT '=== Creare tabele pentru sistemul de notițe ===';
PRINT '';

-- 1. Creează tabelul Notes
PRINT '1. Creare tabel Notes...';
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
    
    CREATE NONCLUSTERED INDEX [IX_Notes_CourseId] ON [dbo].[Notes] ([CourseId]);
    CREATE NONCLUSTERED INDEX [IX_Notes_StudentId] ON [dbo].[Notes] ([StudentId]);
    CREATE NONCLUSTERED INDEX [IX_Notes_Status] ON [dbo].[Notes] ([Status]);
    CREATE NONCLUSTERED INDEX [IX_Notes_CreatedDate] ON [dbo].[Notes] ([CreatedDate] DESC);
    
    PRINT '   ✓ Tabelul Notes a fost creat cu succes!';
END
ELSE
BEGIN
    PRINT '   ⚠ Tabelul Notes există deja.';
END
GO

-- 2. Creează tabelul NoteVotes
PRINT '';
PRINT '2. Creare tabel NoteVotes...';
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
    
    CREATE UNIQUE NONCLUSTERED INDEX [IX_NoteVotes_NoteId_UserId] 
        ON [dbo].[NoteVotes] ([NoteId], [UserId]);
    CREATE NONCLUSTERED INDEX [IX_NoteVotes_NoteId] ON [dbo].[NoteVotes] ([NoteId]);
    CREATE NONCLUSTERED INDEX [IX_NoteVotes_UserId] ON [dbo].[NoteVotes] ([UserId]);
    
    PRINT '   ✓ Tabelul NoteVotes a fost creat cu succes!';
END
ELSE
BEGIN
    PRINT '   ⚠ Tabelul NoteVotes există deja.';
END
GO

-- 3. Creează tabelul NoteShares
PRINT '';
PRINT '3. Creare tabel NoteShares...';
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
    
    CREATE UNIQUE NONCLUSTERED INDEX [IX_NoteShares_NoteId_SharedWithUserId] 
        ON [dbo].[NoteShares] ([NoteId], [SharedWithUserId]);
    CREATE NONCLUSTERED INDEX [IX_NoteShares_NoteId] ON [dbo].[NoteShares] ([NoteId]);
    CREATE NONCLUSTERED INDEX [IX_NoteShares_OwnerId] ON [dbo].[NoteShares] ([OwnerId]);
    CREATE NONCLUSTERED INDEX [IX_NoteShares_SharedWithUserId] ON [dbo].[NoteShares] ([SharedWithUserId]);
    
    PRINT '   ✓ Tabelul NoteShares a fost creat cu succes!';
END
ELSE
BEGIN
    PRINT '   ⚠ Tabelul NoteShares există deja.';
END
GO

-- 4. Creează tabelul NoteComments
PRINT '';
PRINT '4. Creare tabel NoteComments...';
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
    
    PRINT '   ✓ Tabelul NoteComments a fost creat cu succes!';
END
ELSE
BEGIN
    PRINT '   ⚠ Tabelul NoteComments există deja.';
END
GO

PRINT '';
PRINT '=== Finalizat! Toate tabelele pentru sistemul de notițe au fost create. ===';
GO


