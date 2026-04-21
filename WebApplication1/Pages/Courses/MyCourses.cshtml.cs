using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Uniflow.Data;
using Uniflow.Models;

namespace Uniflow.Pages.Courses
{
    [Authorize(Roles = "Student,Admin,Profesor")]
    public class MyCoursesModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public MyCoursesModel(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public IList<CourseViewModel> MyCourses { get; set; } = new List<CourseViewModel>();

        public async Task OnGetAsync()
        {
            var currentUserId = _userManager.GetUserId(User);

            // Găsim toate cursurile la care studentul este înscris
            var enrollments = await _context.CourseEnrollments
                .Where(e => e.StudentId == currentUserId)
                .Include(e => e.Course)
                .ThenInclude(c => c.Profesor)
                .OrderByDescending(e => e.EnrollmentDate)
                .ToListAsync();

            // Obține toate ID-urile de profesori
            var profesorIds = enrollments
                .Where(e => e.Course != null && !string.IsNullOrEmpty(e.Course.ProfesorId))
                .Select(e => e.Course!.ProfesorId!)
                .Distinct()
                .ToList();

            // Încarcă profilele profesorilor
            var profesorProfiles = await _context.UserProfiles
                .Where(p => profesorIds.Contains(p.UserId))
                .ToDictionaryAsync(p => p.UserId, p => p.FullName);

            foreach (var enrollment in enrollments)
            {
                if (enrollment.Course != null)
                {
                    string profesorName = "N/A";
                    if (!string.IsNullOrEmpty(enrollment.Course.ProfesorId))
                    {
                        // Folosește numele complet din UserProfile dacă există
                        if (profesorProfiles.ContainsKey(enrollment.Course.ProfesorId))
                        {
                            profesorName = profesorProfiles[enrollment.Course.ProfesorId];
                        }
                        else if (enrollment.Course.Profesor != null)
                        {
                            profesorName = enrollment.Course.Profesor.UserName ?? enrollment.Course.Profesor.Email ?? "N/A";
                        }
                    }

                    MyCourses.Add(new CourseViewModel
                    {
                        Id = enrollment.Course.Id,
                        Title = enrollment.Course.Title,
                        Description = enrollment.Course.Description,
                        ProfesorEmail = profesorName,
                        CreatedDate = enrollment.Course.CreatedDate,
                        EnrollmentDate = enrollment.EnrollmentDate
                    });
                }
            }
        }

        public class CourseViewModel
        {
            public int Id { get; set; }
            public string Title { get; set; } = string.Empty;
            public string? Description { get; set; }
            public string ProfesorEmail { get; set; } = string.Empty;
            public DateTime CreatedDate { get; set; }
            public DateTime EnrollmentDate { get; set; }
        }
    }
}

