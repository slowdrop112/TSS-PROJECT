using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Uniflow.Data;
using Uniflow.Models;
using Uniflow.Services;

namespace Uniflow.Pages.Profile
{
    public class PublicModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly GamificationService _gamificationService;

        public PublicModel(ApplicationDbContext context, UserManager<IdentityUser> userManager, GamificationService gamificationService)
        {
            _context = context;
            _userManager = userManager;
            _gamificationService = gamificationService;
        }

        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty; // Maybe hide or show based on privacy? Let's show for now as it identifies the student.
        public int XP { get; set; }
        public int Rank { get; set; }
        public DateTime EnrollmentDate { get; set; } // Estimativ, based on user creation or strict logic
        public List<Badge> Badges { get; set; } = new List<Badge>();
        public int TotalNotes { get; set; }
        public int TotalUpvotesReceived { get; set; }

        public async Task<IActionResult> OnGetAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var profile = await _context.UserProfiles
                .FirstOrDefaultAsync(p => p.UserId == id);

            UserName = profile != null && (!string.IsNullOrEmpty(profile.FirstName) || !string.IsNullOrEmpty(profile.LastName))
                ? $"{profile.FirstName} {profile.LastName}"
                : user.UserName ?? "Utilizator";

            Email = user.Email ?? "";
            XP = profile?.XP ?? 0;
            
            // Get Rank
            Rank = await _gamificationService.GetUserRankAsync(id);

            // Get Badges
            Badges = await _context.UserBadges
                .Where(ub => ub.UserId == id)
                .Include(ub => ub.Badge)
                .Select(ub => ub.Badge!)
                .ToListAsync();

            // Stats
            TotalNotes = await _context.Notes.CountAsync(n => n.StudentId == id && n.Status == "Approved");
            
            // Total upvotes received on all notes
            var userNotesIds = await _context.Notes.Where(n => n.StudentId == id).Select(n => n.Id).ToListAsync();
            if (userNotesIds.Any())
            {
                TotalUpvotesReceived = await _context.NoteVotes
                    .CountAsync(v => userNotesIds.Contains(v.NoteId) && v.IsUpvote);
            }
            else
            {
                TotalUpvotesReceived = 0;
            }

            return Page();
        }
    }
}
