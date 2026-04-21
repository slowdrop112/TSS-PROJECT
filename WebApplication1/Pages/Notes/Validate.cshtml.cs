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
    [Authorize(Roles = "Profesor,Admin")]
    public class ValidateModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly GamificationService _gamificationService;
        private readonly Uniflow.Services.NotificationService _notificationService;

        public ValidateModel(ApplicationDbContext context, UserManager<IdentityUser> userManager, GamificationService gamificationService, Uniflow.Services.NotificationService notificationService)
        {
            _context = context;
            _userManager = userManager;
            _gamificationService = gamificationService;
            _notificationService = notificationService;
        }

        public IList<NoteViewModel> PendingNotes { get; set; } = new List<NoteViewModel>();
        public IList<NoteViewModel> ApprovedNotes { get; set; } = new List<NoteViewModel>();
        public IList<NoteViewModel> RejectedNotes { get; set; } = new List<NoteViewModel>();

        [BindProperty(SupportsGet = true)]
        public string? Tab { get; set; } = "pending"; // "pending", "approved", "rejected"

        public async Task OnGetAsync()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return;

            var currentUserId = currentUser.Id;
            var isAdmin = User.IsInRole("Admin");

            // Obține cursurile profesorului (folosim Category pentru email)
            var myCourseIds = new List<int>();
            if (isAdmin)
            {
                // Admin vede toate notițele
                myCourseIds = await _context.Courses.Select(c => c.Id).ToListAsync();
            }
            else
            {
                // Profesor vede doar notițele pentru cursurile sale
                myCourseIds = await _context.Courses
                    .Where(c => c.ProfesorId == currentUserId)
                    .Select(c => c.Id)
                    .ToListAsync();
            }

            // Notițe în așteptare
            var pendingQuery = _context.Notes
                .Include(n => n.Course)
                .Include(n => n.Student)
                .Include(n => n.Votes)
                .Where(n => myCourseIds.Contains(n.CourseId) && n.Status == "Pending");

            var pendingData = await pendingQuery
                .OrderByDescending(n => n.CreatedDate)
                .ToListAsync();

            PendingNotes = new List<NoteViewModel>();
            foreach (var n in pendingData)
            {
                var studentName = "N/A";
                if (!string.IsNullOrEmpty(n.StudentId))
                {
                    var profile = await _context.UserProfiles
                        .Where(p => p.UserId == n.StudentId)
                        .Select(p => new { p.FirstName, p.LastName })
                        .FirstOrDefaultAsync();
                    
                    if (profile != null && !string.IsNullOrWhiteSpace(profile.FirstName) && !string.IsNullOrWhiteSpace(profile.LastName))
                    {
                        studentName = $"{profile.FirstName} {profile.LastName}";
                    }
                    else
                    {
                        studentName = n.Student?.Email ?? "N/A";
                    }
                }

                PendingNotes.Add(new NoteViewModel
                {
                    Id = n.Id,
                    Title = n.Title,
                    Content = n.Content ?? "",
                    CourseTitle = n.Course != null ? n.Course.Title : "N/A",
                    StudentName = studentName,
                    StudentId = n.StudentId,
                    CreatedDate = n.CreatedDate,
                    Status = n.Status,
                    Upvotes = n.Votes.Count(v => v.IsUpvote),
                    Downvotes = n.Votes.Count(v => !v.IsUpvote),
                    Score = n.Votes.Count(v => v.IsUpvote) - n.Votes.Count(v => !v.IsUpvote)
                });
            }

            // Notițe aprobate
            var approvedQuery = _context.Notes
                .Include(n => n.Course)
                .Include(n => n.Student)
                .Include(n => n.Votes)
                .Where(n => myCourseIds.Contains(n.CourseId) && n.Status == "Approved");

            var approvedData = await approvedQuery
                .OrderByDescending(n => n.ValidationDate ?? n.CreatedDate)
                .ToListAsync();

            ApprovedNotes = new List<NoteViewModel>();
            foreach (var n in approvedData)
            {
                var studentName = "N/A";
                if (!string.IsNullOrEmpty(n.StudentId))
                {
                    var profile = await _context.UserProfiles
                        .Where(p => p.UserId == n.StudentId)
                        .Select(p => new { p.FirstName, p.LastName })
                        .FirstOrDefaultAsync();
                    
                    if (profile != null && !string.IsNullOrWhiteSpace(profile.FirstName) && !string.IsNullOrWhiteSpace(profile.LastName))
                    {
                        studentName = $"{profile.FirstName} {profile.LastName}";
                    }
                    else
                    {
                        studentName = n.Student?.Email ?? "N/A";
                    }
                }

                ApprovedNotes.Add(new NoteViewModel
                {
                    Id = n.Id,
                    Title = n.Title,
                    Content = n.Content ?? "",
                    CourseTitle = n.Course != null ? n.Course.Title : "N/A",
                    StudentName = studentName,
                    StudentId = n.StudentId,
                    CreatedDate = n.CreatedDate,
                    Status = n.Status,
                    Upvotes = n.Votes.Count(v => v.IsUpvote),
                    Downvotes = n.Votes.Count(v => !v.IsUpvote),
                    Score = n.Votes.Count(v => v.IsUpvote) - n.Votes.Count(v => !v.IsUpvote),
                    ValidationDate = n.ValidationDate
                });
            }

            // Notițe respinse
            var rejectedQuery = _context.Notes
                .Include(n => n.Course)
                .Include(n => n.Student)
                .Include(n => n.Votes)
                .Where(n => myCourseIds.Contains(n.CourseId) && n.Status == "Rejected");

            var rejectedData = await rejectedQuery
                .OrderByDescending(n => n.ValidationDate ?? n.CreatedDate)
                .ToListAsync();

            RejectedNotes = new List<NoteViewModel>();
            foreach (var n in rejectedData)
            {
                var studentName = "N/A";
                if (!string.IsNullOrEmpty(n.StudentId))
                {
                    var profile = await _context.UserProfiles
                        .Where(p => p.UserId == n.StudentId)
                        .Select(p => new { p.FirstName, p.LastName })
                        .FirstOrDefaultAsync();
                    
                    if (profile != null && !string.IsNullOrWhiteSpace(profile.FirstName) && !string.IsNullOrWhiteSpace(profile.LastName))
                    {
                        studentName = $"{profile.FirstName} {profile.LastName}";
                    }
                    else
                    {
                        studentName = n.Student?.Email ?? "N/A";
                    }
                }

                RejectedNotes.Add(new NoteViewModel
                {
                    Id = n.Id,
                    Title = n.Title,
                    Content = n.Content ?? "",
                    CourseTitle = n.Course != null ? n.Course.Title : "N/A",
                    StudentName = studentName,
                    StudentId = n.StudentId,
                    CreatedDate = n.CreatedDate,
                    Status = n.Status,
                    Upvotes = n.Votes.Count(v => v.IsUpvote),
                    Downvotes = n.Votes.Count(v => !v.IsUpvote),
                    Score = n.Votes.Count(v => v.IsUpvote) - n.Votes.Count(v => !v.IsUpvote),
                    ValidationDate = n.ValidationDate
                });
            }
        }

        public async Task<IActionResult> OnPostApproveAsync(int noteId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToPage("/Account/Login");
            }

            var note = await _context.Notes
                .Include(n => n.Course)
                .FirstOrDefaultAsync(n => n.Id == noteId);

            if (note == null)
            {
                return NotFound();
            }

            // Verifică dacă profesorul este proprietarul cursului sau este Admin
            var isAdmin = User.IsInRole("Admin");
            var isOwner = isAdmin || (note.Course != null && !string.IsNullOrEmpty(note.Course.ProfesorId) && note.Course.ProfesorId == currentUser.Id);

            if (!isOwner)
            {
                return Forbid();
            }

            note.Status = "Approved";
            note.ValidatedByUserId = currentUser.Id;
            note.ValidationDate = DateTime.Now;

            await _context.SaveChangesAsync();

            // Acordă XP pentru aprobare
            await _gamificationService.AwardXPAsync(
                note.StudentId,
                GamificationService.XP_NOTE_APPROVED,
                $"Notiță aprobată: {note.Title}");

            // Acordă XP suplimentar bazat pe voturile existente (dacă notița are deja voturi)
            await _gamificationService.AwardXPFromNoteVotesAsync(
                note.Id,
                note.StudentId,
                note.Title);

            // Notifică studentul că notița a fost aprobată
            var courseTitle = note.Course?.Title ?? "curs";
            await _notificationService.NotifyNoteApprovedAsync(
                note.Id,
                note.StudentId,
                note.Title,
                courseTitle);

            TempData["SuccessMessage"] = "Notița a fost aprobată! Studentul a primit " + GamificationService.XP_NOTE_APPROVED + " XP pentru aprobare.";
            return RedirectToPage("./Validate");
        }

        public async Task<IActionResult> OnPostRejectAsync(int noteId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToPage("/Account/Login");
            }

            var note = await _context.Notes
                .Include(n => n.Course)
                .FirstOrDefaultAsync(n => n.Id == noteId);

            if (note == null)
            {
                return NotFound();
            }

            // Verifică dacă profesorul este proprietarul cursului sau este Admin
            var isAdmin = User.IsInRole("Admin");
            var isOwner = isAdmin || (note.Course != null && !string.IsNullOrEmpty(note.Course.ProfesorId) && note.Course.ProfesorId == currentUser.Id);

            if (!isOwner)
            {
                return Forbid();
            }

            note.Status = "Rejected";
            note.ValidatedByUserId = currentUser.Id;
            note.ValidationDate = DateTime.Now;

            await _context.SaveChangesAsync();

            // Notifică studentul că notița a fost respinsă
            var courseTitle = note.Course?.Title ?? "curs";
            await _notificationService.NotifyNoteRejectedAsync(
                note.Id,
                note.StudentId,
                note.Title,
                courseTitle);

            TempData["SuccessMessage"] = "Notița a fost respinsă.";
            return RedirectToPage("./Validate");
        }

        public class NoteViewModel
        {
            public int Id { get; set; }
            public string Title { get; set; } = string.Empty;
            public string Content { get; set; } = string.Empty;
            public string CourseTitle { get; set; } = string.Empty;
            public string StudentName { get; set; } = string.Empty;
            public string StudentId { get; set; } = string.Empty;
            public DateTime CreatedDate { get; set; }
            public string Status { get; set; } = string.Empty;
            public int Upvotes { get; set; }
            public int Downvotes { get; set; }
            public int Score { get; set; }
            public DateTime? ValidationDate { get; set; }
        }
    }
}


