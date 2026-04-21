using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Uniflow.Data;
using Uniflow.Models;
using Uniflow.Services;

namespace Uniflow.Pages.Notes
{
    [Authorize(Roles = "Student")]
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly GamificationService _gamificationService;
        private readonly Uniflow.Services.NotificationService _notificationService;

        public CreateModel(ApplicationDbContext context, UserManager<IdentityUser> userManager, GamificationService gamificationService, Uniflow.Services.NotificationService notificationService)
        {
            _context = context;
            _userManager = userManager;
            _gamificationService = gamificationService;
            _notificationService = notificationService;
        }

        public SelectList Courses { get; set; } = default!;

        [BindProperty]
        public Note Note { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync()
        {
            var currentUserId = _userManager.GetUserId(User);
            if (currentUserId == null)
            {
                return RedirectToPage("/Account/Login");
            }

            // Obține doar cursurile la care studentul este înscris
            var enrolledCourseIds = await _context.CourseEnrollments
                .Where(e => e.StudentId == currentUserId)
                .Select(e => e.CourseId)
                .ToListAsync();

            var courses = await _context.Courses
                .Where(c => enrolledCourseIds.Contains(c.Id))
                .OrderBy(c => c.Title)
                .ToListAsync();

            Courses = new SelectList(courses, "Id", "Title");
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var currentUserId = _userManager.GetUserId(User);
            if (currentUserId == null)
            {
                return RedirectToPage("/Account/Login");
            }

            // Setează StudentId înainte de validare (pentru că este setat programatic)
            Note.StudentId = currentUserId;
            Note.CreatedDate = DateTime.Now;
            Note.Status = "Pending";

            // Elimină eroarea de validare pentru StudentId (îl setăm programatic)
            ModelState.Remove("Note.StudentId");
            ModelState.Remove("Note.CreatedDate");
            ModelState.Remove("Note.Status");

            // Verifică dacă CourseId este valid
            if (Note.CourseId == 0)
            {
                ModelState.AddModelError("Note.CourseId", "Trebuie să selectezi un curs.");
                var enrolledCourseIds = await _context.CourseEnrollments
                    .Where(e => e.StudentId == currentUserId)
                    .Select(e => e.CourseId)
                    .ToListAsync();
                var courses = await _context.Courses
                    .Where(c => enrolledCourseIds.Contains(c.Id))
                    .OrderBy(c => c.Title)
                    .ToListAsync();
                Courses = new SelectList(courses, "Id", "Title");
                return Page();
            }

            // Verifică dacă studentul este înscris la curs
            var isEnrolled = await _context.CourseEnrollments
                .AnyAsync(e => e.StudentId == currentUserId && e.CourseId == Note.CourseId);

            if (!isEnrolled)
            {
                ModelState.AddModelError("Note.CourseId", "Nu ești înscris la acest curs.");
                var enrolledCourseIds = await _context.CourseEnrollments
                    .Where(e => e.StudentId == currentUserId)
                    .Select(e => e.CourseId)
                    .ToListAsync();
                var courses = await _context.Courses
                    .Where(c => enrolledCourseIds.Contains(c.Id))
                    .OrderBy(c => c.Title)
                    .ToListAsync();
                Courses = new SelectList(courses, "Id", "Title");
                return Page();
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
                Courses = new SelectList(courses, "Id", "Title");
                
                // Log erorile de validare pentru debugging
                foreach (var error in ModelState)
                {
                    foreach (var err in error.Value.Errors)
                    {
                        TempData["ErrorMessage"] = $"Eroare la {error.Key}: {err.ErrorMessage}";
                    }
                }
                
                return Page();
            }

            try
            {
                _context.Notes.Add(Note);
                await _context.SaveChangesAsync();

                // Notifică profesorul că a fost postată o notiță nouă
                var userProfile = await _context.UserProfiles.FirstOrDefaultAsync(p => p.UserId == currentUserId);
                var studentName = userProfile != null 
                    ? $"{userProfile.FirstName} {userProfile.LastName}" 
                    : (await _userManager.GetUserAsync(User))?.UserName ?? "Un student";
                
                await _notificationService.NotifyProfessorOfNewNoteAsync(
                    Note.CourseId,
                    Note.Id,
                    Note.Title,
                    studentName);

                TempData["SuccessMessage"] = "Notița a fost creată cu succes! Va fi validată de profesor și vei primi XP după aprobare.";
                return RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Eroare la crearea notiței: " + ex.Message;
                
                var enrolledCourseIds = await _context.CourseEnrollments
                    .Where(e => e.StudentId == currentUserId)
                    .Select(e => e.CourseId)
                    .ToListAsync();
                var courses = await _context.Courses
                    .Where(c => enrolledCourseIds.Contains(c.Id))
                    .OrderBy(c => c.Title)
                    .ToListAsync();
                Courses = new SelectList(courses, "Id", "Title");
                
                return Page();
            }
        }
    }
}


