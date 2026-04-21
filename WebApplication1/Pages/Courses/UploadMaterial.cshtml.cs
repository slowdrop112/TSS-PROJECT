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
    public class UploadMaterialModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<UploadMaterialModel> _logger;
        private readonly Uniflow.Services.NotificationService _notificationService;

        public UploadMaterialModel(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager,
            IWebHostEnvironment environment,
            ILogger<UploadMaterialModel> logger,
            Uniflow.Services.NotificationService notificationService)
        {
            _context = context;
            _userManager = userManager;
            _environment = environment;
            _logger = logger;
            _notificationService = notificationService;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public Course? Course { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Selectați un fișier")]
            [Display(Name = "Fișier")]
            public IFormFile? File { get; set; }

            [StringLength(500, ErrorMessage = "Descrierea nu poate depăși 500 caractere")]
            [Display(Name = "Descriere (opțional)")]
            public string? Description { get; set; }
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

            if (!ModelState.IsValid || Input.File == null || Input.File.Length == 0)
            {
                ModelState.AddModelError(string.Empty, "Selectați un fișier valid.");
                return Page();
            }

            try
            {
                // Creează folder-ul pentru materialele cursului dacă nu există
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "courses", courseId.ToString());
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                // Generează un nume unic pentru fișier (păstrează extensia originală)
                var fileExtension = Path.GetExtension(Input.File.FileName);
                var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                // Salvează fișierul
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await Input.File.CopyToAsync(stream);
                }

                // Creează înregistrarea în baza de date
                // FilePath trebuie să fie relativ la WebRootPath pentru a funcționa cu DownloadMaterial
                var relativePath = Path.Combine("uploads", "courses", courseId.ToString(), uniqueFileName).Replace("\\", "/");
                var material = new CourseMaterial
                {
                    FileName = Input.File.FileName, // Numele original pentru download
                    FilePath = relativePath,
                    Description = Input.Description,
                    CourseId = courseId,
                    UploadedByUserId = currentUser.Id,
                    UploadDate = DateTime.Now,
                    FileSize = Input.File.Length,
                    ContentType = Input.File.ContentType ?? "application/octet-stream"
                };

                _context.CourseMaterials.Add(material);
                await _context.SaveChangesAsync();

                // Notifică studenții despre materialul nou
                var userProfile = await _context.UserProfiles.FirstOrDefaultAsync(u => u.UserId == currentUser.Id);
                var teacherName = userProfile != null 
                    ? $"{userProfile.FirstName} {userProfile.LastName}" 
                    : currentUser.UserName;

                await _notificationService.NotifyStudentsOfMaterialAsync(courseId, Input.File.FileName, teacherName);

                TempData["SuccessMessage"] = $"Fișierul '{Input.File.FileName}' a fost încărcat cu succes!";
                return RedirectToPage("./Details", new { id = courseId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Eroare la încărcarea fișierului");
                ModelState.AddModelError(string.Empty, "A apărut o eroare la încărcarea fișierului. Încercați din nou.");
                return Page();
            }
        }
    }
}

