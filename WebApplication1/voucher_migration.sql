BEGIN TRANSACTION;
GO

CREATE TABLE [Courses] (
    [Id] int NOT NULL IDENTITY,
    [Title] nvarchar(200) NOT NULL,
    [Description] nvarchar(1000) NULL,
    [ProfesorId] nvarchar(450) NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    CONSTRAINT [PK_Courses] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Courses_AspNetUsers_ProfesorId] FOREIGN KEY ([ProfesorId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [CourseEnrollments] (
    [Id] int NOT NULL IDENTITY,
    [CourseId] int NOT NULL,
    [StudentId] nvarchar(450) NOT NULL,
    [EnrollmentDate] datetime2 NOT NULL,
    CONSTRAINT [PK_CourseEnrollments] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_CourseEnrollments_AspNetUsers_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_CourseEnrollments_Courses_CourseId] FOREIGN KEY ([CourseId]) REFERENCES [Courses] ([Id]) ON DELETE CASCADE
);
GO

CREATE UNIQUE INDEX [IX_CourseEnrollments_CourseId_StudentId] ON [CourseEnrollments] ([CourseId], [StudentId]);
GO

CREATE INDEX [IX_CourseEnrollments_StudentId] ON [CourseEnrollments] ([StudentId]);
GO

CREATE INDEX [IX_Courses_ProfesorId] ON [Courses] ([ProfesorId]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20251213110124_AddCourseAndCourseEnrollment', N'8.0.11');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [RoleRequests] (
    [Id] int NOT NULL IDENTITY,
    [UserId] nvarchar(450) NOT NULL,
    [RequestedRole] nvarchar(50) NOT NULL,
    [Status] nvarchar(20) NOT NULL,
    [RequestDate] datetime2 NOT NULL,
    [ProcessedDate] datetime2 NULL,
    [ProcessedByUserId] nvarchar(450) NULL,
    CONSTRAINT [PK_RoleRequests] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_RoleRequests_AspNetUsers_ProcessedByUserId] FOREIGN KEY ([ProcessedByUserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_RoleRequests_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);
GO

CREATE INDEX [IX_RoleRequests_ProcessedByUserId] ON [RoleRequests] ([ProcessedByUserId]);
GO

CREATE INDEX [IX_RoleRequests_UserId] ON [RoleRequests] ([UserId]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20251213131351_AddRoleRequests', N'8.0.11');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [CourseMaterials] (
    [Id] int NOT NULL IDENTITY,
    [FileName] nvarchar(255) NOT NULL,
    [FilePath] nvarchar(500) NOT NULL,
    [Description] nvarchar(500) NULL,
    [CourseId] int NOT NULL,
    [UploadedByUserId] nvarchar(450) NOT NULL,
    [UploadDate] datetime2 NOT NULL,
    [FileSize] bigint NOT NULL,
    [ContentType] nvarchar(100) NULL,
    CONSTRAINT [PK_CourseMaterials] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_CourseMaterials_AspNetUsers_UploadedByUserId] FOREIGN KEY ([UploadedByUserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_CourseMaterials_Courses_CourseId] FOREIGN KEY ([CourseId]) REFERENCES [Courses] ([CourseID]) ON DELETE CASCADE
);
GO

CREATE TABLE [Notes] (
    [Id] int NOT NULL IDENTITY,
    [Title] nvarchar(255) NOT NULL,
    [Content] nvarchar(max) NULL,
    [CourseId] int NOT NULL,
    [StudentId] nvarchar(450) NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    [Status] nvarchar(20) NOT NULL DEFAULT N'Pending',
    [ValidatedByUserId] nvarchar(450) NULL,
    [ValidationDate] datetime2 NULL,
    CONSTRAINT [PK_Notes] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Notes_AspNetUsers_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_Notes_AspNetUsers_ValidatedByUserId] FOREIGN KEY ([ValidatedByUserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Notes_Courses_CourseId] FOREIGN KEY ([CourseId]) REFERENCES [Courses] ([CourseID]) ON DELETE CASCADE
);
GO

CREATE TABLE [UserProfiles] (
    [UserId] nvarchar(450) NOT NULL,
    [LastName] nvarchar(100) NOT NULL,
    [FirstName] nvarchar(100) NOT NULL,
    [XP] int NOT NULL DEFAULT 0,
    CONSTRAINT [PK_UserProfiles] PRIMARY KEY ([UserId]),
    CONSTRAINT [FK_UserProfiles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [NoteVotes] (
    [Id] int NOT NULL IDENTITY,
    [NoteId] int NOT NULL,
    [UserId] nvarchar(450) NOT NULL,
    [IsUpvote] bit NOT NULL,
    [VoteDate] datetime2 NOT NULL,
    CONSTRAINT [PK_NoteVotes] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_NoteVotes_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_NoteVotes_Notes_NoteId] FOREIGN KEY ([NoteId]) REFERENCES [Notes] ([Id]) ON DELETE CASCADE
);
GO

CREATE INDEX [IX_CourseMaterials_CourseId] ON [CourseMaterials] ([CourseId]);
GO

CREATE INDEX [IX_CourseMaterials_UploadedByUserId] ON [CourseMaterials] ([UploadedByUserId]);
GO

CREATE INDEX [IX_Notes_CourseId] ON [Notes] ([CourseId]);
GO

CREATE INDEX [IX_Notes_StudentId] ON [Notes] ([StudentId]);
GO

CREATE INDEX [IX_Notes_ValidatedByUserId] ON [Notes] ([ValidatedByUserId]);
GO

CREATE UNIQUE INDEX [IX_NoteVotes_NoteId_UserId] ON [NoteVotes] ([NoteId], [UserId]);
GO

CREATE INDEX [IX_NoteVotes_UserId] ON [NoteVotes] ([UserId]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20251214094249_AddGamificationSystem', N'8.0.11');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260120130407_AddProfesorIdToCourses', N'8.0.11');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260129160616_AddBadgesClean', N'8.0.11');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [Vouchers] (
    [Id] int NOT NULL IDENTITY,
    [Title] nvarchar(200) NOT NULL,
    [Description] nvarchar(1000) NOT NULL,
    [PartnerName] nvarchar(200) NOT NULL,
    [DiscountType] nvarchar(50) NOT NULL,
    [DiscountValue] nvarchar(50) NOT NULL,
    [RequiredLevel] int NOT NULL,
    [ValidityDays] int NOT NULL,
    [IconUrl] nvarchar(500) NULL,
    [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
    CONSTRAINT [PK_Vouchers] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [UserVouchers] (
    [Id] int NOT NULL IDENTITY,
    [UserId] nvarchar(450) NOT NULL,
    [VoucherId] int NOT NULL,
    [Code] nvarchar(50) NOT NULL,
    [AwardedDate] datetime2 NOT NULL,
    [ExpiryDate] datetime2 NOT NULL,
    [IsRedeemed] bit NOT NULL DEFAULT CAST(0 AS bit),
    [RedeemedDate] datetime2 NULL,
    CONSTRAINT [PK_UserVouchers] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_UserVouchers_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_UserVouchers_Vouchers_VoucherId] FOREIGN KEY ([VoucherId]) REFERENCES [Vouchers] ([Id]) ON DELETE NO ACTION
);
GO

CREATE UNIQUE INDEX [IX_UserVouchers_Code] ON [UserVouchers] ([Code]);
GO

CREATE INDEX [IX_UserVouchers_UserId_IsRedeemed] ON [UserVouchers] ([UserId], [IsRedeemed]);
GO

CREATE INDEX [IX_UserVouchers_VoucherId] ON [UserVouchers] ([VoucherId]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260204092909_AddVoucherSystem', N'8.0.11');
GO

COMMIT;
GO

