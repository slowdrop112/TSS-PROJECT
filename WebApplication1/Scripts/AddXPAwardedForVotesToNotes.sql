-- Adaugă coloana XPAwardedForVotes în tabelul Notes
-- Această coloană ține minte cât XP a fost deja acordat pentru voturi,
-- pentru a evita dublarea XP-ului când notița primește voturi noi

IF NOT EXISTS (
    SELECT 1 
    FROM sys.columns 
    WHERE object_id = OBJECT_ID(N'[dbo].[Notes]') 
    AND name = 'XPAwardedForVotes'
)
BEGIN
    ALTER TABLE [dbo].[Notes]
    ADD [XPAwardedForVotes] INT NOT NULL DEFAULT 0;
    
    PRINT 'Coloana XPAwardedForVotes a fost adăugată cu succes în tabelul Notes.';
END
ELSE
BEGIN
    PRINT 'Coloana XPAwardedForVotes există deja în tabelul Notes.';
END
GO

