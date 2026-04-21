USE [Uniflow] -- Înlocuiește cu numele bazei tale de date
GO

-- Verifică dacă tabelul UserNotifications există deja
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[UserNotifications]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[UserNotifications] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [UserId] NVARCHAR(450) NOT NULL,
        [Title] NVARCHAR(200) NOT NULL,
        [Message] NVARCHAR(2000) NOT NULL,
        [Type] NVARCHAR(20) NOT NULL CONSTRAINT [DF_UserNotifications_Type] DEFAULT ('info'),
        [IsRead] BIT NOT NULL CONSTRAINT [DF_UserNotifications_IsRead] DEFAULT (0),
        [CreatedDate] DATETIME2 NOT NULL CONSTRAINT [DF_UserNotifications_CreatedDate] DEFAULT (SYSUTCDATETIME()),
        [LinkUrl] NVARCHAR(500) NULL,
        CONSTRAINT [PK_UserNotifications] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_UserNotifications_AspNetUsers] FOREIGN KEY ([UserId])
            REFERENCES [dbo].[AspNetUsers] ([Id])
            ON DELETE CASCADE
    );

    CREATE NONCLUSTERED INDEX [IX_UserNotifications_UserId_IsRead]
        ON [dbo].[UserNotifications] ([UserId], [IsRead]);

    CREATE NONCLUSTERED INDEX [IX_UserNotifications_UserId_CreatedDate]
        ON [dbo].[UserNotifications] ([UserId], [CreatedDate] DESC);

    PRINT 'Tabelul UserNotifications a fost creat cu succes!';
END
ELSE
BEGIN
    PRINT 'Tabelul UserNotifications există deja.';
END
GO

