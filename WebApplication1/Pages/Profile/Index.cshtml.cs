using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Uniflow.Data;

using Uniflow.Models;
using Uniflow.Services;

namespace Uniflow.Pages.Profile
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly GamificationService _gamificationService;
        private readonly BadgeService _badgeService;

        public IndexModel(ApplicationDbContext context, UserManager<IdentityUser> userManager, GamificationService gamificationService, BadgeService badgeService)
        {
            _context = context;
            _userManager = userManager;
            _gamificationService = gamificationService;
            _badgeService = badgeService;
        }

        public int XP { get; set; }
        public int Rank { get; set; }
        public string? UserEmail { get; set; }
        public string? UserName { get; set; }
        public int TotalCourses { get; set; }

        public int TotalNotes { get; set; }
        public List<Badge> Badges { get; set; } = new List<Badge>();

        public async Task OnGetAsync()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null)
            {
                UserEmail = currentUser.Email;
                UserName = currentUser.UserName;

                // Obține XP-ul utilizatorului
                XP = await _gamificationService.GetXPAsync(currentUser.Id);

                // Ensure badges exist and refresh dynamic badges (Ranks, etc.)
                await _badgeService.EnsureBadgesExistAsync();
                await _badgeService.RefreshDynamicBadgesAsync();
                await _badgeService.CheckPersistentBadgesAsync(currentUser.Id);

                // Load User Badges
                Badges = await _context.UserBadges
                    .Include(ub => ub.Badge)
                    .Where(ub => ub.UserId == currentUser.Id)
                    .Select(ub => ub.Badge!)
                    .ToListAsync();

                // Obține poziția în clasament
                Rank = await _gamificationService.GetUserRankAsync(currentUser.Id);

                var roles = await _userManager.GetRolesAsync(currentUser);
                var isStudent = roles.Contains("Student");
                var isProfessor = roles.Contains("Profesor");
                var isAdmin = roles.Contains("Admin");

                if (isStudent)
                {
                    // Student: Cursuri înrolate
                    TotalCourses = await _context.CourseEnrollments
                        .CountAsync(e => e.StudentId == currentUser.Id);
                    
                    try {
                        TotalNotes = await _context.Notes.CountAsync(n => n.StudentId == currentUser.Id);
                    } catch { TotalNotes = 0; }
                }
                else if (isProfessor)
                {
                    // Profesor: Cursuri create
                    TotalCourses = await _context.Courses
                        .CountAsync(c => c.ProfesorId == currentUser.Id);
                    
                    // Profesor: Notițe (validări? sau simplu 0 momentan)
                    TotalNotes = 0; 
                }
                else if (isAdmin)
                {
                    // Admin: Statistici globale sistem
                    TotalCourses = await _context.Courses.CountAsync();
                    try {
                        TotalNotes = await _context.Notes.CountAsync();
                    } catch { TotalNotes = 0; }
                }

                // Populate InputModel
                var profile = await _context.UserProfiles.FirstOrDefaultAsync(p => p.UserId == currentUser.Id);
                if (profile != null)
                {
                    Input = new InputModel
                    {
                        FirstName = profile.FirstName,
                        LastName = profile.LastName
                    };
                }
                else
                {
                    Input = new InputModel();
                }
            }
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [System.ComponentModel.DataAnnotations.Display(Name = "Prenume")]
            [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Prenumele este obligatoriu.")]
            [System.ComponentModel.DataAnnotations.StringLength(100, ErrorMessage = "Prenumele nu poate depăși 100 de caractere.")]
            public string FirstName { get; set; }

            [System.ComponentModel.DataAnnotations.Display(Name = "Nume")]
            [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Numele este obligatoriu.")]
            [System.ComponentModel.DataAnnotations.StringLength(100, ErrorMessage = "Numele nu poate depăși 100 de caractere.")]
            public string LastName { get; set; }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            if (!ModelState.IsValid)
            {
                // Reload data for display
                await OnGetAsync();
                return Page();
            }

            var profile = await _context.UserProfiles.FirstOrDefaultAsync(p => p.UserId == currentUser.Id);
            if (profile == null)
            {
                // Create profile if it doesn't exist (should normally exist due to GamificationService)
                profile = new Models.UserProfile
                {
                    UserId = currentUser.Id,
                    FirstName = Input.FirstName,
                    LastName = Input.LastName,
                    XP = 0
                };
                _context.UserProfiles.Add(profile);
            }
            else
            {
                profile.FirstName = Input.FirstName;
                profile.LastName = Input.LastName;
            }

            await _context.SaveChangesAsync();

            StatusMessage = "Profilul a fost actualizat cu succes!";
            return RedirectToPage();
        }

        [TempData]
        public string StatusMessage { get; set; }
    }
}

