using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Uniflow.Data;
using Uniflow.Models;
using Uniflow.Services;

namespace WebApplication1.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly GamificationService _gamificationService;

        public IndexModel(
            ILogger<IndexModel> logger,
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager,
            GamificationService gamificationService)
        {
            _logger = logger;
            _context = context;
            _userManager = userManager;
            _gamificationService = gamificationService;
        }

        public int TotalCourses { get; set; }
        public int XP { get; set; }
        public int Rank { get; set; }
        public string? UserDisplayName { get; set; }
        public string? UserRole { get; set; }
        
        // Role-Specific Counters
        public int TotalStudents { get; set; }
        public int PendingValidationsCount { get; set; }
        public int PendingRoleRequestsCount { get; set; }
        public int TotalUsersCount { get; set; }

        public List<UserNotification> UnreadNotifications { get; set; } = new();

        public async Task OnGetAsync()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                var currentUser = await _userManager.GetUserAsync(User);
                 if (currentUser != null)
                {
                    var roles = await _userManager.GetRolesAsync(currentUser);
                    UserRole = roles.FirstOrDefault() ?? "Student";
                    var isProfessor = roles.Contains("Profesor");
                    var isAdmin = roles.Contains("Admin");

                    // Set Display Name
                    var profile = await _context.UserProfiles.FirstOrDefaultAsync(p => p.UserId == currentUser.Id);
                    if (profile != null && !string.IsNullOrWhiteSpace(profile.FullName))
                    {
                        UserDisplayName = profile.FullName;
                    }
                    else
                    {
                        UserDisplayName = currentUser.UserName ?? currentUser.Email ?? "Utilizator";
                    }
                    
                    // Fetch Unread Notifications (All Users)
                    UnreadNotifications = await _context.UserNotifications
                        .Where(n => n.UserId == currentUser.Id && !n.IsRead)
                        .OrderByDescending(n => n.CreatedDate)
                        .Take(5)
                        .ToListAsync();

                    // Role-Specific Logic
                    if (isAdmin)
                    {
                        // Admin Stats
                        PendingRoleRequestsCount = await _context.RoleRequests
                            .CountAsync(r => r.Status == "Pending");
                        
                        TotalUsersCount = await _userManager.Users.CountAsync();
                        TotalCourses = await _context.Courses.CountAsync();
                    }
                    else if (isProfessor)
                    {
                        // Professor Stats
                        var myCourseIds = await _context.Courses
                            .Where(c => c.ProfesorId == currentUser.Id)
                            .Select(c => c.Id)
                            .ToListAsync();

                        PendingValidationsCount = await _context.Notes
                            .CountAsync(n => myCourseIds.Contains(n.CourseId) && n.Status == "Pending");
                        
                        TotalCourses = myCourseIds.Count;
                        TotalStudents = await _context.CourseEnrollments
                            .CountAsync(e => myCourseIds.Contains(e.CourseId));
                    }
                    else
                    {
                        // Student Stats
                        TotalCourses = await _context.CourseEnrollments.CountAsync(e => e.StudentId == currentUser.Id);
                        XP = await _gamificationService.GetXPAsync(currentUser.Id);
                        Rank = await _gamificationService.GetUserRankAsync(currentUser.Id);
                    }
                }
            }
        }
    }
}
