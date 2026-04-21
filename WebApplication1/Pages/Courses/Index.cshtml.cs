using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Uniflow.Data;
using Uniflow.Models;

namespace Uniflow.Pages.Courses
{
    [Authorize] // Permite accesul pentru toți utilizatorii autentificați
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly Uniflow.Services.GamificationService _gamificationService;

        public IndexModel(ApplicationDbContext context, UserManager<IdentityUser> userManager, Uniflow.Services.GamificationService gamificationService)
        {
            _context = context;
            _userManager = userManager;
            _gamificationService = gamificationService;
        }

        public IList<CourseViewModel> Courses { get; set; } = new List<CourseViewModel>();

        [BindProperty(SupportsGet = true)]
        public string? SearchString { get; set; }

        public bool IsProfesor { get; set; }

        public async Task OnGetAsync()
        {
            var currentUserId = _userManager.GetUserId(User);
            IsProfesor = User.IsInRole("Profesor") || User.IsInRole("Admin");
            
            var coursesQuery = _context.Courses.AsQueryable();

            // Căutare după titlu sau descriere
            if (!string.IsNullOrEmpty(SearchString))
            {
                coursesQuery = coursesQuery.Where(c => 
                    c.Title.Contains(SearchString) || 
                    (c.Description != null && c.Description.Contains(SearchString)));
            }

            var courses = await coursesQuery.OrderByDescending(c => c.CreatedDate).ToListAsync();

            // Obține toate ID-urile unice de profesori
            var profesorIds = courses
                .Where(c => !string.IsNullOrEmpty(c.ProfesorId))
                .Select(c => c.ProfesorId!)
                .Distinct()
                .ToList();

            // Query pentru profesori și profilele lor
            var profesorNames = new Dictionary<string, string>();
            if (profesorIds.Any())
            {
                var profesors = await _context.Users
                    .Where(u => profesorIds.Contains(u.Id))
                    .Select(u => new { u.Id, u.Email, u.UserName })
                    .ToListAsync();

                // Încarcă profilele utilizatorilor
                var userProfiles = await _context.UserProfiles
                    .Where(p => profesorIds.Contains(p.UserId))
                    .Select(p => new { p.UserId, p.FirstName, p.LastName })
                    .ToListAsync();

                // Creează un dicționar pentru profilele utilizatorilor
                var profileDict = userProfiles.ToDictionary(p => p.UserId, p => $"{p.FirstName} {p.LastName}");

                foreach (var prof in profesors)
                {
                    // Folosește numele complet din UserProfile dacă există, altfel email/username
                    profesorNames[prof.Id] = profileDict.ContainsKey(prof.Id) 
                        ? profileDict[prof.Id] 
                        : prof.UserName ?? prof.Email ?? "N/A";
                }
            }

            // Verificăm pentru fiecare curs dacă studentul este deja înscris sau dacă profesorul este proprietarul
            foreach (var course in courses)
            {
                var isEnrolled = false;
                var isOwner = false;

                if (User.IsInRole("Student") || User.IsInRole("Admin"))
                {
                    isEnrolled = await _context.CourseEnrollments
                        .AnyAsync(e => e.CourseId == course.Id && e.StudentId == currentUserId);
                }

                // Verificăm dacă profesorul este proprietarul cursului
                if (IsProfesor && !string.IsNullOrEmpty(course.ProfesorId))
                {
                    isOwner = course.ProfesorId == currentUserId;
                }

                // Obținem numele profesorului din dictionary
                string profesorEmail = "N/A";
                if (!string.IsNullOrEmpty(course.ProfesorId) && profesorNames.ContainsKey(course.ProfesorId))
                {
                    profesorEmail = profesorNames[course.ProfesorId];
                }

                Courses.Add(new CourseViewModel
                {
                    Id = course.Id,
                    Title = course.Title,
                    Description = course.Description,
                    ProfesorEmail = profesorEmail,
                    CreatedDate = course.CreatedDate,
                    IsEnrolled = isEnrolled,
                    IsOwner = isOwner
                });
            }
        }

        // Handler pentru înscrierea directă din listă
        public async Task<IActionResult> OnPostEnrollAsync(int courseId)
        {
            var currentUserId = _userManager.GetUserId(User);
            
            if (currentUserId == null)
            {
                return Forbid();
            }

            // Verificăm dacă studentul este deja înscris
            var alreadyEnrolled = await _context.CourseEnrollments
                .AnyAsync(e => e.CourseId == courseId && e.StudentId == currentUserId);

            if (alreadyEnrolled)
            {
                TempData["ErrorMessage"] = "Ești deja înscris la acest curs!";
                return RedirectToPage("./Index");
            }

            // Verificăm dacă cursul există
            var course = await _context.Courses.FindAsync(courseId);
            if (course == null)
            {
                return NotFound();
            }

            // Creăm înscrierea
            var enrollment = new CourseEnrollment
            {
                CourseId = course.Id,
                StudentId = currentUserId,
                EnrollmentDate = DateTime.Now
            };

            _context.CourseEnrollments.Add(enrollment);
            await _context.SaveChangesAsync();

            // Acordă XP pentru înscriere la curs
            await _gamificationService.AwardXPAsync(currentUserId!, Uniflow.Services.GamificationService.XP_ENROLL_COURSE, $"Înscriere la curs ID: {courseId}");

            TempData["SuccessMessage"] = $"Te-ai înscris cu succes la curs! Ai primit {Uniflow.Services.GamificationService.XP_ENROLL_COURSE} XP!";
            return RedirectToPage("./Index");
        }

        public class CourseViewModel
        {
            public int Id { get; set; }
            public string Title { get; set; } = string.Empty;
            public string? Description { get; set; }
            public string ProfesorEmail { get; set; } = string.Empty;
            public DateTime CreatedDate { get; set; }
            public bool IsEnrolled { get; set; }
            public bool IsOwner { get; set; }
        }
    }
}

