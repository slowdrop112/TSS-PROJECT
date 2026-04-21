using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using Uniflow.Data;
using Uniflow.Models;

namespace Uniflow.Pages.Courses
{
    [Authorize(Roles = "Profesor,Admin")]
    public class CreateAnnouncementModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly Uniflow.Services.NotificationService _notificationService;

        public CreateAnnouncementModel(ApplicationDbContext context, UserManager<IdentityUser> userManager, Uniflow.Services.NotificationService notificationService)
        {
            _context = context;
            _userManager = userManager;
            _notificationService = notificationService;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public Course? Course { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Titlul este obligatoriu")]
            [StringLength(500, ErrorMessage = "Titlul nu poate depăși 500 caractere")]
            [Display(Name = "Titlu")]
            public string Title { get; set; } = string.Empty;

            [Required(ErrorMessage = "Conținutul este obligatoriu")]
            [Display(Name = "Conținut")]
            public string Content { get; set; } = string.Empty;

            [Display(Name = "Anunț Important")]
            public bool IsImportant { get; set; } = false;
        }

        public async Task<IActionResult> OnGetAsync(int courseId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToPage("/Account/Login");
            }

            Course = await _context.Courses.FindAsync(courseId);
            if (Course == null)
            {
                return NotFound();
            }

            // Verifică dacă utilizatorul este proprietarul cursului sau Admin
            var isAdmin = User.IsInRole("Admin");
            var isOwner = isAdmin || (!string.IsNullOrEmpty(Course.ProfesorId) && Course.ProfesorId == currentUser.Id);

            if (!isOwner)
            {
                return Forbid();
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int courseId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToPage("/Account/Login");
            }

            Course = await _context.Courses.FindAsync(courseId);
            if (Course == null)
            {
                return NotFound();
            }

            // Verifică dacă utilizatorul este proprietarul cursului sau Admin
            var isAdmin = User.IsInRole("Admin");
            var isOwner = isAdmin || (!string.IsNullOrEmpty(Course.ProfesorId) && Course.ProfesorId == currentUser.Id);

            if (!isOwner)
            {
                return Forbid();
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                var announcement = new CourseAnnouncement
                {
                    Title = Input.Title,
                    Content = Input.Content,
                    CourseId = courseId,
                    PostedByUserId = currentUser.Id,
                    PostedDate = DateTime.Now,
                    IsImportant = Input.IsImportant
                };

                _context.CourseAnnouncements.Add(announcement);
                await _context.SaveChangesAsync();

                // Notifică toți studenții înscriși la curs despre anunțul nou
                var userProfile = await _context.UserProfiles.FirstOrDefaultAsync(u => u.UserId == currentUser.Id);
                var teacherName = userProfile != null 
                    ? $"{userProfile.FirstName} {userProfile.LastName}" 
                    : currentUser.UserName;

                await _notificationService.NotifyStudentsOfAnnouncementAsync(courseId, Input.Title, teacherName);

                TempData["SuccessMessage"] = "Anunțul a fost postat cu succes!";
                return RedirectToPage("./Details", new { id = courseId, tab = "announcements" });
            }
            catch
            {
                ModelState.AddModelError(string.Empty, "A apărut o eroare la crearea anunțului. Încercați din nou.");
                return Page();
            }
        }
    }
}

