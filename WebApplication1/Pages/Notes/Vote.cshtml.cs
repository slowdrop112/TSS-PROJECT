using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Uniflow.Data;
using Uniflow.Models;
using Uniflow.Services;

namespace Uniflow.Pages.Notes
{
    [Authorize(Roles = "Student")]
    [ValidateAntiForgeryToken]
    public class VoteModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly GamificationService _gamificationService;
        private readonly ILogger<VoteModel> _logger;
        private readonly Uniflow.Services.NotificationService _notificationService;

        public VoteModel(ApplicationDbContext context, UserManager<IdentityUser> userManager, GamificationService gamificationService, ILogger<VoteModel> logger, Uniflow.Services.NotificationService notificationService)
        {
            _context = context;
            _userManager = userManager;
            _gamificationService = gamificationService;
            _logger = logger;
            _notificationService = notificationService;
        }

        public async Task<IActionResult> OnPostAsync(int noteId, string voteType)
        {
            var currentUserId = _userManager.GetUserId(User);
            if (currentUserId == null)
            {
                return new JsonResult(new { success = false, message = "Utilizator neautentificat" });
            }

            // Verifică dacă notița există
            var note = await _context.Notes
                .Include(n => n.Votes)
                .FirstOrDefaultAsync(n => n.Id == noteId);

            if (note == null)
            {
                return new JsonResult(new { success = false, message = "Notița nu există" });
            }

            // Nu poți vota propria notiță
            if (note.StudentId == currentUserId)
            {
                return new JsonResult(new { success = false, message = "Nu poți vota propria notiță" });
            }

            // Verifică dacă utilizatorul este înscris la curs
            var isEnrolled = await _context.CourseEnrollments
                .AnyAsync(e => e.StudentId == currentUserId && e.CourseId == note.CourseId);

            if (!isEnrolled)
            {
                // Verifică dacă notița este partajată cu utilizatorul
                var isShared = await _context.NoteShares
                    .AnyAsync(s => s.NoteId == noteId && s.SharedWithUserId == currentUserId);

                if (!isShared)
                {
                    return new JsonResult(new { success = false, message = "Nu ești autorizat să votezi această notiță" });
                }
            }

            bool isUpvote = voteType.ToLower() == "upvote";

            // Verifică dacă utilizatorul a votat deja
            var existingVote = await _context.NoteVotes
                .FirstOrDefaultAsync(v => v.NoteId == noteId && v.UserId == currentUserId);

            if (existingVote != null)
            {
                // Dacă votează același tip, elimină votul
                if (existingVote.IsUpvote == isUpvote)
                {
                    _context.NoteVotes.Remove(existingVote);
                    await _context.SaveChangesAsync();

                    // Recalculează scorul
                    var upvotes = await _context.NoteVotes.CountAsync(v => v.NoteId == noteId && v.IsUpvote);
                    var downvotes = await _context.NoteVotes.CountAsync(v => v.NoteId == noteId && !v.IsUpvote);
                    var score = upvotes - downvotes;

                    return new JsonResult(new { success = true, score = score, upvotes = upvotes, downvotes = downvotes, userVote = (bool?)null });
                }
                else
                {
                    // Schimbă tipul votului
                    existingVote.IsUpvote = isUpvote;
                    existingVote.VoteDate = DateTime.Now;
                }
            }
            else
            {
                // Creează vot nou
                var newVote = new NoteVote
                {
                    NoteId = noteId,
                    UserId = currentUserId,
                    IsUpvote = isUpvote,
                    VoteDate = DateTime.Now
                };
                _context.NoteVotes.Add(newVote);
            }

            await _context.SaveChangesAsync();

            // Recalculează scorul
            var finalUpvotes = await _context.NoteVotes.CountAsync(v => v.NoteId == noteId && v.IsUpvote);
            var finalDownvotes = await _context.NoteVotes.CountAsync(v => v.NoteId == noteId && !v.IsUpvote);
            var finalScore = finalUpvotes - finalDownvotes;

            // Dacă notița este validată, acordă XP bazat pe voturile actuale
            // (recalculăm și acordăm doar diferența față de XP-ul deja acordat)
            if (note.Status == "Approved")
            {
                try
                {
                    await _gamificationService.AwardXPFromNoteVotesAsync(
                        noteId,
                        note.StudentId,
                        note.Title);
                }
                catch (Exception ex)
                {
                    // Log eroarea dar nu blochează votul
                    _logger.LogError(ex, $"Eroare la acordarea XP pentru voturi la notița {noteId}");
                }
            }

            // Notifică autorul notiței când primește un vot (doar dacă votul este nou sau schimbat, nu șters)
            var userVote = await _context.NoteVotes
                .FirstOrDefaultAsync(v => v.NoteId == noteId && v.UserId == currentUserId);
            
            if (userVote != null)
            {
                try
                {
                    await _notificationService.NotifyNoteReceivedVoteAsync(
                        noteId,
                        note.StudentId,
                        note.Title,
                        userVote.IsUpvote);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Eroare la trimiterea notificării de vot pentru notița {noteId}");
                }
            }

            return new JsonResult(new 
            { 
                success = true, 
                score = finalScore,
                upvotes = finalUpvotes,
                downvotes = finalDownvotes,
                userVote = userVote?.IsUpvote
            });
        }
    }
}

