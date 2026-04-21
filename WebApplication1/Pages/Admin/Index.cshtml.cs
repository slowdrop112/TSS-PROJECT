using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Uniflow.Data;
using Uniflow.Services;

namespace Uniflow.Pages.Admin
{
    // Access only for users with the "Admin" role
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly DashboardService _dashboardService;
        private readonly NotificationService _notificationService;

        public IndexModel(
            UserManager<IdentityUser> userManager,
            ApplicationDbContext context,
            DashboardService dashboardService,
            NotificationService notificationService)
        {
            _userManager = userManager;
            _context = context;
            _dashboardService = dashboardService;
            _notificationService = notificationService;

            // Initialize non-nullable properties
            Users = new List<UserViewModel>();
            Statistics = new DashboardStatistics();
            Trends = new DashboardTrends();
            TopCourses = new List<TopCourse>();
            TopStudents = new List<TopStudent>();
            GlobalTitle = string.Empty;
            GlobalMessage = string.Empty;
            GlobalType = "info";
            StatusMessage = string.Empty;
        }

        // Global Notification properties
        [BindProperty]
        public string GlobalTitle { get; set; }
        [BindProperty]
        public string GlobalMessage { get; set; }
        [BindProperty]
        public string GlobalType { get; set; }
        public string StatusMessage { get; set; }

        // List of users for display
        public List<UserViewModel> Users { get; set; }

        // Dashboard statistics
        public DashboardStatistics Statistics { get; set; }
        public DashboardTrends Trends { get; set; }
        public List<TopCourse> TopCourses { get; set; }
        public List<TopStudent> TopStudents { get; set; }

        // Properties for Razor page compatibility
        public int TotalStudents => Statistics.TotalStudents;
        public int TotalProfessors => Statistics.TotalProfessors;
        public int TotalCourses => Statistics.TotalCourses;
        public int TotalEnrollments => Statistics.TotalEnrollments;

        public async Task OnGetAsync()
        {
            await LoadDataAsync();
        }

        public async Task<IActionResult> OnPostSendGlobalNotificationAsync()
        {
            if (string.IsNullOrEmpty(GlobalTitle) || string.IsNullOrEmpty(GlobalMessage))
            {
                StatusMessage = "Error: Titlul și mesajul sunt obligatorii.";
                await LoadDataAsync();
                return Page();
            }

            await _notificationService.NotifyAllUsersAsync(GlobalTitle, GlobalMessage, GlobalType);
            StatusMessage = "Succes: Notificarea globală a fost trimisă tuturor utilizatorilor.";

            return RedirectToPage();
        }

        private async Task LoadDataAsync()
        {
            // Get all users
            var allUsers = await _userManager.Users.ToListAsync();
            Users.Clear();

            foreach (var user in allUsers)
            {
                var roles = user != null ? await _userManager.GetRolesAsync(user) : new List<string>();
                Users.Add(new UserViewModel
                {
                    Id = user?.Id ?? "",
                    Email = user?.Email ?? "N/A",
                    Roles = string.Join(", ", roles),
                    UserName = user?.UserName ?? "N/A",
                    IsCurrentUser = user?.UserName == User.Identity?.Name
                });
            }

            // Get dashboard stats
            Statistics = await _dashboardService.GetDashboardStatisticsAsync() ?? new DashboardStatistics();
            Trends = await _dashboardService.GetDashboardTrendsAsync(30) ?? new DashboardTrends();
            TopCourses = await _dashboardService.GetTopCoursesAsync(5) ?? new List<TopCourse>();
            TopStudents = await _dashboardService.GetTopStudentsAsync(5) ?? new List<TopStudent>();
        }

        public async Task<IActionResult> OnPostCleanupUniflowUsersAsync()
        {
            var currentUserId = _userManager.GetUserId(User);
            var usersToDelete = await _userManager.Users
                .Where(u => u.Email.EndsWith("@uniflow.com"))
                .ToListAsync();

            int count = 0;

            foreach (var user in usersToDelete)
            {
                // Skip if current user
                if (user.Id == currentUserId) continue;

                // Skip if admin role
                if (await _userManager.IsInRoleAsync(user, "Admin")) continue;
                
                // Skip if username is "admin" (just in case)
                if (user.UserName?.ToLower() == "admin") continue;

                await DeleteUserDataAsync(user.Id);
                var result = await _userManager.DeleteAsync(user);
                if (result.Succeeded) count++;
            }

            StatusMessage = $"Succes: Au fost șterși {count} utilizatori @uniflow.com (non-admin).";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostPopulateDemoDataAsync()
        {
            int studentsCreated = 0;
            int professorsCreated = 0;
            string password = "Uniflow123!";

            // 1. Create 10 Students (stud-1 to stud-10)
            for (int i = 1; i <= 10; i++)
            {
                if (await CreateDemoUserAsync($"stud-{i}@uniflow.com", password, "Student", "Student", $"{i}"))
                {
                    studentsCreated++;
                }
            }

            // 2. Create 5 Professors (prof-1 to prof-5)
            for (int i = 1; i <= 5; i++)
            {
                if (await CreateDemoUserAsync($"prof-{i}@uniflow.com", password, "Profesor", "Profesor", $"{i}"))
                {
                    professorsCreated++;
                }
            }

            StatusMessage = $"Succes: Au fost adăugați {studentsCreated} studenți și {professorsCreated} profesori.";
            return RedirectToPage();
        }

        private async Task<bool> CreateDemoUserAsync(string email, string password, string role, string firstName, string lastName)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                user = new IdentityUser 
                { 
                    UserName = email, 
                    Email = email, 
                    EmailConfirmed = true 
                };
                
                var result = await _userManager.CreateAsync(user, password);
                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, role);
                    
                    // Create Profile
                    var profile = new Uniflow.Models.UserProfile
                    {
                        UserId = user.Id,
                        FirstName = firstName,
                        LastName = lastName,
                        XP = 0
                    };
                    _context.UserProfiles.Add(profile);
                    await _context.SaveChangesAsync();
                    return true;
                }
            }
            return false;
        }

        private async Task DeleteUserDataAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return;

            var roles = await _userManager.GetRolesAsync(user);
            var primaryRole = roles.FirstOrDefault();

            // 1. Common cleanup
            var roleRequests = await _context.RoleRequests.Where(r => r.UserId == userId).ToListAsync();
            _context.RoleRequests.RemoveRange(roleRequests);

            var materials = await _context.CourseMaterials.Where(m => m.UploadedByUserId == userId).ToListAsync();
            _context.CourseMaterials.RemoveRange(materials);

            var announcements = await _context.CourseAnnouncements.Where(a => a.PostedByUserId == userId).ToListAsync();
            _context.CourseAnnouncements.RemoveRange(announcements);

            var notifications = await _context.UserNotifications.Where(n => n.UserId == userId).ToListAsync();
            _context.UserNotifications.RemoveRange(notifications);

            // 2. Role specific
            if (primaryRole == "Student")
            {
                // Nullify validations
                var validatedNotes = await _context.Notes.Where(n => n.ValidatedByUserId == userId).ToListAsync();
                foreach (var note in validatedNotes) { note.ValidatedByUserId = null; note.ValidationDate = null; }

                // Enrollments
                var enrollments = await _context.CourseEnrollments.Where(e => e.StudentId == userId).ToListAsync();
                _context.CourseEnrollments.RemoveRange(enrollments);

                // Notes (Owned)
                var ownedNotes = await _context.Notes.Where(n => n.StudentId == userId).ToListAsync();
                _context.Notes.RemoveRange(ownedNotes);

                // Interactions
                var votes = await _context.NoteVotes.Where(v => v.UserId == userId).ToListAsync();
                _context.NoteVotes.RemoveRange(votes);
                var comments = await _context.NoteComments.Where(c => c.AuthorId == userId).ToListAsync();
                _context.NoteComments.RemoveRange(comments);
                var shares = await _context.NoteShares.Where(s => s.OwnerId == userId || s.SharedWithUserId == userId).ToListAsync();
                _context.NoteShares.RemoveRange(shares);
            }
            else if (primaryRole == "Profesor")
            {
                // Nullify validations
                var validatedNotes = await _context.Notes.Where(n => n.ValidatedByUserId == userId).ToListAsync();
                foreach (var note in validatedNotes) { note.ValidatedByUserId = null; note.ValidationDate = null; }

                // Courses
                var courses = await _context.Courses.Where(c => c.ProfesorId == userId).ToListAsync();
                _context.Courses.RemoveRange(courses);
            }

            // Profile
            var profile = await _context.UserProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
            if (profile != null) _context.UserProfiles.Remove(profile);

            await _context.SaveChangesAsync();
        }

        // Helper class for users display
        public class UserViewModel
        {
            public string Id { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string Roles { get; set; } = string.Empty;
            public string UserName { get; set; } = string.Empty;
            public bool IsCurrentUser { get; set; }
        }
    }
}