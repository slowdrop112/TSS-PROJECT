using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Uniflow.Data;
using Uniflow.Models;

namespace Uniflow.Pages.Notes
{
    [Authorize(Roles = "Student")]
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
        public Note Note { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var currentUserId = _userManager.GetUserId(User);
            if (currentUserId == null)
            {
                return RedirectToPage("/Account/Login");
            }

            var note = await _context.Notes
                .Include(n => n.Course)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (note == null)
            {
                return NotFound();
            }

            // Verifică dacă utilizatorul este proprietarul notiței
            if (note.StudentId != currentUserId)
            {
                return Forbid();
            }

            Note = note;

            var enrolledCourseIds = await _context.CourseEnrollments
                .Where(e => e.StudentId == currentUserId)
                .Select(e => e.CourseId)
                .ToListAsync();

            var courses = await _context.Courses
                .Where(c => enrolledCourseIds.Contains(c.Id))
                .OrderBy(c => c.Title)
                .ToListAsync();

            ViewData["CourseId"] = new SelectList(courses, "Id", "Title", note.CourseId);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var currentUserId = _userManager.GetUserId(User);
            if (currentUserId == null)
            {
                return RedirectToPage("/Account/Login");
            }

            // Verifică dacă utilizatorul este proprietarul notiței
            var existingNote = await _context.Notes.FindAsync(Note.Id);
            if (existingNote == null)
            {
                return NotFound();
            }

            // Doar proprietarul poate edita (nu utilizatorii care au acces prin partajare)
            if (existingNote.StudentId != currentUserId)
            {
                return Forbid();
            }

            if (!ModelState.IsValid)
            {
                var enrolledCourseIds = await _context.CourseEnrollments
                    .Where(e => e.StudentId == currentUserId)
                    .Select(e => e.CourseId)
                    .ToListAsync();
                var courses = await _context.Courses
                    .Where(c => enrolledCourseIds.Contains(c.Id))
                    .OrderBy(c => c.Title)
                    .ToListAsync();
                ViewData["CourseId"] = new SelectList(courses, "Id", "Title", Note.CourseId);
                return Page();
            }

            // Actualizează doar câmpurile permise
            existingNote.Title = Note.Title;
            existingNote.Content = Note.Content;
            existingNote.CourseId = Note.CourseId;
            existingNote.Status = "Pending"; // Resetează statusul la Pending când se editează

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!NoteExists(Note.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            TempData["SuccessMessage"] = "Notița a fost actualizată cu succes!";
            return RedirectToPage("./Index");
        }

        private bool NoteExists(int id)
        {
            return _context.Notes.Any(e => e.Id == id);
        }
    }
}


