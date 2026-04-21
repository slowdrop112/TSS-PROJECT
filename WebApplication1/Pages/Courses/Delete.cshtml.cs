using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Uniflow.Data;
using Uniflow.Models;

namespace Uniflow.Pages.Courses
{
    [Authorize(Roles = "Profesor,Admin")]
    public class DeleteModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public DeleteModel(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [BindProperty]
        public Course Course { get; set; } = default!;

        public string? ProfesorEmail { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var course = await _context.Courses
                .Include(c => c.Profesor)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (course == null)
            {
                return NotFound();
            }

            // Verificăm dacă utilizatorul curent este profesorul cursului sau Admin
            var currentUserId = _userManager.GetUserId(User);
            var isAdmin = User.IsInRole("Admin");
            var isProfesor = User.IsInRole("Profesor");

            // Verificăm dacă profesorul este proprietarul cursului
            var isOwner = false;
            if (isProfesor && !string.IsNullOrEmpty(currentUserId) && !string.IsNullOrEmpty(course.ProfesorId))
            {
                isOwner = course.ProfesorId == currentUserId;
            }

            if (!isAdmin && (!isProfesor || !isOwner))
            {
                return Forbid(); // Nu poate șterge cursurile altor profesori
            }

            Course = course;
            
            // Obținem email-ul profesorului
            ProfesorEmail = "N/A";
            if (course.Profesor != null)
            {
                ProfesorEmail = course.Profesor.UserName ?? course.Profesor.Email ?? "N/A";
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var course = await _context.Courses.FindAsync(id);

            if (course == null)
            {
                return NotFound();
            }

            // Verificăm din nou autorizarea
            var currentUserId = _userManager.GetUserId(User);
            var isAdmin = User.IsInRole("Admin");
            var isProfesor = User.IsInRole("Profesor");

            // Verificăm dacă profesorul este proprietarul cursului
            var isOwner = false;
            if (isProfesor && !string.IsNullOrEmpty(currentUserId) && !string.IsNullOrEmpty(course.ProfesorId))
            {
                isOwner = course.ProfesorId == currentUserId;
            }

            if (!isAdmin && (!isProfesor || !isOwner))
            {
                return Forbid(); // Nu poate șterge cursurile altor profesori
            }

            // Ștergem toate înscrierile asociate cu acest curs
            var enrollments = await _context.CourseEnrollments
                .Where(e => e.CourseId == course.Id)
                .ToListAsync();
            _context.CourseEnrollments.RemoveRange(enrollments);

            // Ștergem cursul
            _context.Courses.Remove(course);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Cursul a fost șters cu succes.";
            return RedirectToPage("./Index");
        }
    }
}





