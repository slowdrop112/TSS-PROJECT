using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Uniflow.Data;
using Uniflow.Models;

namespace Uniflow.Services
{
    /// <summary>
    /// Serviciu pentru colectarea și calcularea statisticilor pentru dashboard-ul admin
    /// </summary>
    public class DashboardService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<DashboardService> _logger;

        public DashboardService(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager,
            ILogger<DashboardService> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        /// <summary>
        /// Returnează toate statisticile pentru dashboard-ul admin
        /// </summary>
        public async Task<DashboardStatistics> GetDashboardStatisticsAsync()
        {
            try
            {
                var stats = new DashboardStatistics
                {
                    // Statistici utilizatori
                    TotalStudents = (await _userManager.GetUsersInRoleAsync("Student")).Count,
                    TotalProfessors = (await _userManager.GetUsersInRoleAsync("Profesor")).Count,
                    TotalAdmins = (await _userManager.GetUsersInRoleAsync("Admin")).Count,
                    TotalUsers = await _userManager.Users.CountAsync(),

                    // Statistici cursuri
                    TotalCourses = await _context.Courses.CountAsync(),
                    PublishedCourses = await _context.Courses.CountAsync(c => c.IsPublished),
                    UnpublishedCourses = await _context.Courses.CountAsync(c => !c.IsPublished),
                    TotalEnrollments = await _context.CourseEnrollments.CountAsync(),

                    // Statistici notițe (cu gestionare erori dacă tabelul nu există)
                    TotalNotes = await SafeCountAsync(() => _context.Notes.CountAsync()),
                    PendingNotes = await SafeCountAsync(() => _context.Notes.CountAsync(n => n.Status == "Pending")),
                    ApprovedNotes = await SafeCountAsync(() => _context.Notes.CountAsync(n => n.Status == "Approved")),
                    RejectedNotes = await SafeCountAsync(() => _context.Notes.CountAsync(n => n.Status == "Rejected")),

                    // Statistici voturi (cu gestionare erori dacă tabelul nu există)
                    TotalVotes = await SafeCountAsync(() => _context.NoteVotes.CountAsync()),
                    TotalUpvotes = await SafeCountAsync(() => _context.NoteVotes.CountAsync(v => v.IsUpvote)),
                    TotalDownvotes = await SafeCountAsync(() => _context.NoteVotes.CountAsync(v => !v.IsUpvote)),

                    // Statistici partajări (cu gestionare erori dacă tabelul nu există)
                    TotalShares = await SafeCountAsync(() => _context.NoteShares.CountAsync()),

                    // Statistici materiale (cu gestionare erori dacă tabelul nu există)
                    TotalMaterials = await SafeCountAsync(() => _context.CourseMaterials.CountAsync()),

                    // Statistici cereri roluri
                    PendingRoleRequests = await SafeCountAsync(() => _context.RoleRequests.CountAsync(r => r.Status == "Pending")),

                    // Statistici XP (gamificare) (cu gestionare erori dacă tabelul nu există)
                    TotalXP = await SafeSumAsync(() => _context.UserProfiles.SumAsync(p => (int?)p.XP)),
                    AverageXP = await SafeAverageXPAsync(),
                    TopXPUser = await SafeGetTopXPUserAsync()
                };

                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Eroare la calcularea statisticilor dashboard");
                throw;
            }
        }

        /// <summary>
        /// Helper method pentru a număra în siguranță, returnând 0 dacă tabelul nu există
        /// </summary>
        private async Task<int> SafeCountAsync(Func<Task<int>> countAction)
        {
            try
            {
                return await countAction();
            }
            catch (Microsoft.Data.SqlClient.SqlException ex) when (ex.Message.Contains("Invalid object name"))
            {
                _logger.LogWarning(ex, "Tabelul nu există încă în baza de date. Rulează scripturile SQL pentru a crea tabelele.");
                return 0;
            }
        }

        /// <summary>
        /// Helper method pentru a calcula suma în siguranță
        /// </summary>
        private async Task<int> SafeSumAsync(Func<Task<int?>> sumAction)
        {
            try
            {
                return await sumAction() ?? 0;
            }
            catch (Microsoft.Data.SqlClient.SqlException ex) when (ex.Message.Contains("Invalid object name"))
            {
                _logger.LogWarning(ex, "Tabelul nu există încă în baza de date. Rulează scripturile SQL pentru a crea tabelele.");
                return 0;
            }
        }

        /// <summary>
        /// Helper method pentru a calcula media XP în siguranță
        /// </summary>
        private async Task<int> SafeAverageXPAsync()
        {
            try
            {
                if (await _context.UserProfiles.AnyAsync())
                {
                    return (int)await _context.UserProfiles.AverageAsync(p => (double)p.XP);
                }
                return 0;
            }
            catch (Microsoft.Data.SqlClient.SqlException ex) when (ex.Message.Contains("Invalid object name"))
            {
                _logger.LogWarning(ex, "Tabelul UserProfiles nu există încă în baza de date. Rulează scripturile SQL pentru a crea tabelele.");
                return 0;
            }
        }

        /// <summary>
        /// Helper method pentru a obține top XP user în siguranță
        /// </summary>
        private async Task<TopXPUser?> SafeGetTopXPUserAsync()
        {
            try
            {
                return await _context.UserProfiles
                    .OrderByDescending(p => p.XP)
                    .Select(p => new TopXPUser { UserId = p.UserId, XP = p.XP })
                    .FirstOrDefaultAsync();
            }
            catch (Microsoft.Data.SqlClient.SqlException ex) when (ex.Message.Contains("Invalid object name"))
            {
                _logger.LogWarning(ex, "Tabelul UserProfiles nu există încă în baza de date. Rulează scripturile SQL pentru a crea tabelele.");
                return null;
            }
        }

        /// <summary>
        /// Returnează statistici pentru ultimele N zile (pentru grafice/trends)
        /// </summary>
        public async Task<DashboardTrends> GetDashboardTrendsAsync(int days = 30)
        {
            try
            {
                var startDate = DateTime.UtcNow.AddDays(-days);

                // Pentru utilizatori, folosim UserManager (Identity nu are CreatedDate standard)
                var allUsers = await _userManager.Users.ToListAsync();
                var newUsersCount = allUsers.Count; // Aproximare - Identity nu păstrează CreatedDate standard

                var trends = new DashboardTrends
                {
                    NewUsersLast30Days = newUsersCount, // Aproximare
                    NewCoursesLast30Days = await _context.Courses
                        .CountAsync(c => c.CreatedDate >= startDate),
                    NewEnrollmentsLast30Days = await _context.CourseEnrollments
                        .CountAsync(e => e.EnrollmentDate >= startDate),
                    NewNotesLast30Days = await _context.Notes
                        .CountAsync(n => n.CreatedDate >= startDate),
                    NewVotesLast30Days = await _context.NoteVotes
                        .CountAsync(v => v.VoteDate >= startDate)
                };

                return trends;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Eroare la calcularea trend-urilor dashboard");
                throw;
            }
        }

        /// <summary>
        /// Returnează top cursuri după numărul de înscrieri
        /// </summary>
        public async Task<List<TopCourse>> GetTopCoursesAsync(int count = 5)
        {
            try
            {
                var topCourses = await _context.Courses
                    .Select(c => new TopCourse
                    {
                        CourseId = c.Id,
                        Title = c.Title,
                        EnrollmentCount = _context.CourseEnrollments.Count(e => e.CourseId == c.Id),
                        IsPublished = c.IsPublished
                    })
                    .OrderByDescending(c => c.EnrollmentCount)
                    .Take(count)
                    .ToListAsync();

                return topCourses;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Eroare la obținerea top cursuri");
                throw;
            }
        }

        /// <summary>
        /// Returnează top studenți după XP
        /// </summary>
        public async Task<List<TopStudent>> GetTopStudentsAsync(int count = 5)
        {
            try
            {
                var topProfiles = await _context.UserProfiles
                    .OrderByDescending(p => p.XP)
                    .Take(count)
                    .ToListAsync();

                var topStudents = new List<TopStudent>();
                foreach (var profile in topProfiles)
                {
                    var user = await _userManager.FindByIdAsync(profile.UserId);
                    topStudents.Add(new TopStudent
                    {
                        UserId = profile.UserId,
                        XP = profile.XP,
                        Email = user?.Email ?? "N/A"
                    });
                }

                return topStudents;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Eroare la obținerea top studenți");
                throw;
            }
        }
    }

    /// <summary>
    /// Model pentru statisticile dashboard-ului
    /// </summary>
    public class DashboardStatistics
    {
        // Utilizatori
        public int TotalStudents { get; set; }
        public int TotalProfessors { get; set; }
        public int TotalAdmins { get; set; }
        public int TotalUsers { get; set; }

        // Cursuri
        public int TotalCourses { get; set; }
        public int PublishedCourses { get; set; }
        public int UnpublishedCourses { get; set; }
        public int TotalEnrollments { get; set; }

        // Notițe
        public int TotalNotes { get; set; }
        public int PendingNotes { get; set; }
        public int ApprovedNotes { get; set; }
        public int RejectedNotes { get; set; }

        // Voturi
        public int TotalVotes { get; set; }
        public int TotalUpvotes { get; set; }
        public int TotalDownvotes { get; set; }

        // Partajări
        public int TotalShares { get; set; }

        // Materiale
        public int TotalMaterials { get; set; }

        // Cereri roluri
        public int PendingRoleRequests { get; set; }

        // XP (Gamificare)
        public int TotalXP { get; set; }
        public int AverageXP { get; set; }
        public TopXPUser? TopXPUser { get; set; }
    }

    /// <summary>
    /// Model pentru trend-uri (creșteri în timp)
    /// </summary>
    public class DashboardTrends
    {
        public int NewUsersLast30Days { get; set; }
        public int NewCoursesLast30Days { get; set; }
        public int NewEnrollmentsLast30Days { get; set; }
        public int NewNotesLast30Days { get; set; }
        public int NewVotesLast30Days { get; set; }
    }

    /// <summary>
    /// Model pentru top cursuri
    /// </summary>
    public class TopCourse
    {
        public int CourseId { get; set; }
        public string Title { get; set; } = string.Empty;
        public int EnrollmentCount { get; set; }
        public bool IsPublished { get; set; }
    }

    /// <summary>
    /// Model pentru top studenți
    /// </summary>
    public class TopStudent
    {
        public string UserId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int XP { get; set; }
    }

    /// <summary>
    /// Model pentru utilizatorul cu cel mai mare XP
    /// </summary>
    public class TopXPUser
    {
        public string UserId { get; set; } = string.Empty;
        public int XP { get; set; }
    }
}

