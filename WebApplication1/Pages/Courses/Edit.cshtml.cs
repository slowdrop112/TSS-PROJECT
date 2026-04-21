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
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public EditModel(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [BindProperty]
        public Course Course { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var course = await _context.Courses.FirstOrDefaultAsync(m => m.Id == id);
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
                return Forbid(); // Nu poate edita cursurile altor profesori
            }

            Course = course;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Verificăm din nou autorizarea
            var currentUserId = _userManager.GetUserId(User);
            var isAdmin = User.IsInRole("Admin");
            var isProfesor = User.IsInRole("Profesor");

            var existingCourse = await _context.Courses.FindAsync(Course.Id);
            if (existingCourse == null)
            {
                return NotFound();
            }

            // Verificăm dacă profesorul este proprietarul cursului
            var isOwner = false;
            if (isProfesor && !string.IsNullOrEmpty(currentUserId) && !string.IsNullOrEmpty(existingCourse.ProfesorId))
            {
                isOwner = existingCourse.ProfesorId == currentUserId;
            }

            if (!isAdmin && (!isProfesor || !isOwner))
            {
                return Forbid(); // Nu poate edita cursurile altor profesori
            }

            // Actualizăm doar câmpurile editabile
            existingCourse.Title = Course.Title;
            existingCourse.Description = Course.Description;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CourseExists(Course.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return RedirectToPage("./Index");
        }

        private bool CourseExists(int id)
        {
            return _context.Courses.Any(e => e.Id == id);
        }
    }
}

