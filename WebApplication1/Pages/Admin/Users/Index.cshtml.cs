using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Uniflow.Data;

namespace Uniflow.Pages.Admin.Users
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(ApplicationDbContext context, UserManager<IdentityUser> userManager, ILogger<IndexModel> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        public List<UserViewModel> Users { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? RoleFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? SearchString { get; set; }

        public async Task OnGetAsync()
        {
            var currentUserId = _userManager.GetUserId(User);
            var allUsers = await _userManager.Users.ToListAsync();

            foreach (var user in allUsers)
            {
                var roles = await _userManager.GetRolesAsync(user);
                var primaryRole = roles.FirstOrDefault() ?? "N/A";

                // Apply filters
                if (!string.IsNullOrEmpty(RoleFilter) && primaryRole != RoleFilter)
                    continue;

                if (!string.IsNullOrEmpty(SearchString))
                {
                    var searchLower = SearchString.ToLower();
                    if (!(user.Email?.ToLower().Contains(searchLower) == true ||
                          user.UserName?.ToLower().Contains(searchLower) == true))
                        continue;
                }

                Users.Add(new UserViewModel
                {
                    Id = user.Id,
                    UserName = user.UserName ?? "N/A",
                    Email = user.Email ?? "N/A",
                    Role = primaryRole,
                    EmailConfirmed = user.EmailConfirmed,
                    IsCurrentUser = user.Id == currentUserId
                });
            }

            // Sort by role then by username
            Users = Users.OrderBy(u => u.Role).ThenBy(u => u.UserName).ToList();
        }

        public async Task<IActionResult> OnPostDeleteAsync(string userId)
        {
            var currentUserId = _userManager.GetUserId(User);

            // Prevent deleting yourself
            if (userId == currentUserId)
            {
                TempData["ErrorMessage"] = "Nu poți șterge propriul cont!";
                return RedirectToPage();
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Utilizatorul nu a fost găsit.";
                return RedirectToPage();
            }

            try
            {
                // Delete related data in proper order to avoid FK constraint errors
                var roles = await _userManager.GetRolesAsync(user);
                var primaryRole = roles.FirstOrDefault();

                // Delete role requests for this user
                var roleRequests = await _context.RoleRequests
                    .Where(r => r.UserId == userId)
                    .ToListAsync();
                _context.RoleRequests.RemoveRange(roleRequests);

                // Delete course materials uploaded by this user (has Restrict FK)
                var materials = await _context.CourseMaterials
                    .Where(m => m.UploadedByUserId == userId)
                    .ToListAsync();
                _context.CourseMaterials.RemoveRange(materials);

                // Delete course announcements posted by this user (has Restrict FK)
                var announcements = await _context.CourseAnnouncements
                    .Where(a => a.PostedByUserId == userId)
                    .ToListAsync();
                _context.CourseAnnouncements.RemoveRange(announcements);

                // Delete notifications (no dependencies)
                var notifications = await _context.UserNotifications
                    .Where(n => n.UserId == userId)
                    .ToListAsync();
                _context.UserNotifications.RemoveRange(notifications);

                if (primaryRole == "Student")
                {
                    // Nullify ValidatedByUserId for notes validated by this user (has Restrict FK)
                    var validatedNotes = await _context.Notes
                        .Where(n => n.ValidatedByUserId == userId)
                        .ToListAsync();
                    foreach (var note in validatedNotes)
                    {
                        note.ValidatedByUserId = null;
                        note.ValidationDate = null;
                    }

                    // Delete enrollments first (independent)
                    var enrollments = await _context.CourseEnrollments
                        .Where(e => e.StudentId == userId)
                        .ToListAsync();
                    _context.CourseEnrollments.RemoveRange(enrollments);

                    // Delete notes owned by this student
                    // This will CASCADE delete votes, comments, and shares on THESE notes
                    var ownedNotes = await _context.Notes
                        .Where(n => n.StudentId == userId)
                        .ToListAsync();
                    _context.Notes.RemoveRange(ownedNotes);

                    // After notes are deleted, we need to manually clean up this user's interactions
                    // on OTHER people's notes (which have Restrict FK)
                    
                    // These queries will find items on notes that still exist (not owned by this user)
                    var votes = await _context.NoteVotes
                        .Where(v => v.UserId == userId)
                        .ToListAsync();
                    _context.NoteVotes.RemoveRange(votes);

                    var comments = await _context.NoteComments
                        .Where(c => c.AuthorId == userId)
                        .ToListAsync();
                    _context.NoteComments.RemoveRange(comments);

                    var shares = await _context.NoteShares
                        .Where(s => s.OwnerId == userId || s.SharedWithUserId == userId)
                        .ToListAsync();
                    _context.NoteShares.RemoveRange(shares);

                    // Delete user profile (which contains XP)
                    var profile = await _context.UserProfiles
                        .FirstOrDefaultAsync(p => p.UserId == userId);
                    if (profile != null)
                    {
                        _context.UserProfiles.Remove(profile);
                    }
                }
                else if (primaryRole == "Profesor")
                {
                    // Nullify ValidatedByUserId for notes validated by this professor
                    var validatedNotes = await _context.Notes
                        .Where(n => n.ValidatedByUserId == userId)
                        .ToListAsync();
                    foreach (var note in validatedNotes)
                    {
                        note.ValidatedByUserId = null;
                        note.ValidationDate = null;
                    }

                    // Delete courses (which will cascade to materials, announcements, enrollments)
                    var courses = await _context.Courses
                        .Where(c => c.ProfesorId == userId)
                        .ToListAsync();
                    _context.Courses.RemoveRange(courses);
                    
                    // Delete profile
                    var profile = await _context.UserProfiles
                        .FirstOrDefaultAsync(p => p.UserId == userId);
                    if (profile != null)
                    {
                        _context.UserProfiles.Remove(profile);
                    }
                }
                else
                {
                    // For other roles (Admin, etc.) - just delete profile if exists
                    var profile = await _context.UserProfiles
                        .FirstOrDefaultAsync(p => p.UserId == userId);
                    if (profile != null)
                    {
                        _context.UserProfiles.Remove(profile);
                    }
                }

                // Save all changes before deleting the user
                await _context.SaveChangesAsync();

                // Finally, delete the user from Identity
                var result = await _userManager.DeleteAsync(user);

                if (result.Succeeded)
                {
                    TempData["SuccessMessage"] = $"Utilizatorul {user.UserName ?? user.Email} a fost șters cu succes!";
                    _logger.LogInformation($"Admin {currentUserId} deleted user {userId}");
                }
                else
                {
                    TempData["ErrorMessage"] = $"Eroare la ștergerea utilizatorului: {string.Join(", ", result.Errors.Select(e => e.Description))}";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting user {userId}");
                TempData["ErrorMessage"] = $"A apărut o eroare la ștergerea utilizatorului: {ex.Message}";
            }

            return RedirectToPage();
        }

        public class UserViewModel
        {
            public string Id { get; set; } = string.Empty;
            public string UserName { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string Role { get; set; } = string.Empty;
            public bool EmailConfirmed { get; set; }
            public bool IsCurrentUser { get; set; }
        }
    }
}
