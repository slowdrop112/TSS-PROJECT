-- Script pentru a marca migrația ca aplicată în baza de date
-- Rulează acest script direct în SQL Server Management Studio sau Azure Data Studio

-- Pasul 1: Creează tabelul __EFMigrationsHistory dacă nu există
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[__EFMigrationsHistory]') AND type in (N'U'))
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
    PRINT 'Tabelul __EFMigrationsHistory a fost creat.';
END
ELSE
BEGIN
    PRINT 'Tabelul __EFMigrationsHistory există deja.';
END

-- Pasul 2: Adaugă migrația InitialCreate dacă nu există
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = '20251130091646_InitialCreate')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES ('20251130091646_InitialCreate', '8.0.11');
    PRINT 'Migrația InitialCreate a fost adăugată.';
END

-- Pasul 3: Adaugă migrația AddCourseAndCourseEnrollment dacă nu există
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = '20251213110124_AddCourseAndCourseEnrollment')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES ('20251213110124_AddCourseAndCourseEnrollment', '8.0.11');
    PRINT 'Migrația AddCourseAndCourseEnrollment a fost marcată ca aplicată.';
END
ELSE
BEGIN
    PRINT 'Migrația AddCourseAndCourseEnrollment este deja marcată ca aplicată.';
END

