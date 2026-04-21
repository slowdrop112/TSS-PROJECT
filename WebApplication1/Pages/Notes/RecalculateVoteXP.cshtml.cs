using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Uniflow.Data;
using Uniflow.Services;

namespace Uniflow.Pages.Notes
{
    [Authorize(Roles = "Admin")]
    public class RecalculateVoteXPModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly GamificationService _gamificationService;
        private readonly ILogger<RecalculateVoteXPModel> _logger;

        public RecalculateVoteXPModel(
            ApplicationDbContext context,
            GamificationService gamificationService,
            ILogger<RecalculateVoteXPModel> logger)
        {
            _context = context;
            _gamificationService = gamificationService;
            _logger = logger;
        }

        public int ProcessedNotes { get; set; }
        public int TotalXPAwarded { get; set; }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                // Obține toate notițele validate care au voturi
                var approvedNotes = await _context.Notes
                    .Where(n => n.Status == "Approved")
                    .Include(n => n.Votes)
                    .ToListAsync();

                int totalXP = 0;
                int processed = 0;

                foreach (var note in approvedNotes)
                {
                    var upvotes = note.Votes.Count(v => v.IsUpvote);
                    var downvotes = note.Votes.Count(v => !v.IsUpvote);

                    if (upvotes > 0 || downvotes > 0)
                    {
                        var xpToAward = _gamificationService.CalculateXPFromVotes(upvotes, downvotes);
                        
                        if (xpToAward > 0)
                        {
                            await _gamificationService.AwardXPAsync(
                                note.StudentId,
                                xpToAward,
                                $"Recalculare XP pentru notiță: {note.Title} ({upvotes} upvotes, {downvotes} downvotes)");
                            
                            totalXP += xpToAward;
                            processed++;
                        }
                    }
                }

                ProcessedNotes = processed;
                TotalXPAwarded = totalXP;

                TempData["SuccessMessage"] = $"Procesat {processed} notițe. Total XP acordat: {totalXP}";
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Eroare la recalcularea XP pentru voturi");
                TempData["ErrorMessage"] = "A apărut o eroare: " + ex.Message;
                return Page();
            }
        }
    }
}

