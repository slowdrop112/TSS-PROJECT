using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Uniflow.Data;
using Uniflow.Models;

namespace Uniflow.Pages.Notes
{
    [Authorize(Roles = "Student")]
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
                .Include(n => n.Student)
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
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
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

            var note = await _context.Notes.FindAsync(id);

            if (note != null)
            {
                // Verifică dacă utilizatorul este proprietarul notiței
                if (note.StudentId != currentUserId)
                {
                    return Forbid();
                }

                // Șterge voturile asociate
                var votes = await _context.NoteVotes
                    .Where(v => v.NoteId == id)
                    .ToListAsync();
                _context.NoteVotes.RemoveRange(votes);

                // Șterge partajările asociate
                var shares = await _context.NoteShares
                    .Where(s => s.NoteId == id)
                    .ToListAsync();
                _context.NoteShares.RemoveRange(shares);

                _context.Notes.Remove(note);
                await _context.SaveChangesAsync();
            }

            TempData["SuccessMessage"] = "Notița a fost ștearsă cu succes!";
            return RedirectToPage("./Index");
        }
    }
}


