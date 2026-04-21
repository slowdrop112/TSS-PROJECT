using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Uniflow.Data;
using Uniflow.Models;

namespace Uniflow.Pages.Courses
{
    [Authorize]
    public class DownloadMaterialModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<DownloadMaterialModel> _logger;

        public DownloadMaterialModel(
            ApplicationDbContext context,
            IWebHostEnvironment environment,
            ILogger<DownloadMaterialModel> logger)
        {
            _context = context;
            _environment = environment;
            _logger = logger;
        }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var material = await _context.CourseMaterials
                .Include(m => m.Course)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (material == null || material.Course == null)
            {
                return NotFound();
            }

            // Verifică dacă utilizatorul este înscris la curs sau este profesor/admin
            var userManager = HttpContext.RequestServices.GetRequiredService<UserManager<IdentityUser>>();
            var currentUser = await userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Unauthorized();
            }

            var currentUserId = currentUser.Id;
            var isEnrolled = await _context.CourseEnrollments
                .AnyAsync(e => e.StudentId == currentUserId && e.CourseId == material.CourseId);

            var isProfesorOrAdmin = User.IsInRole("Profesor") || User.IsInRole("Admin");
            var isOwner = !string.IsNullOrEmpty(material.Course.ProfesorId) && material.Course.ProfesorId == currentUserId;

            if (!isEnrolled && !isProfesorOrAdmin && !isOwner)
            {
                return Forbid();
            }

            try
            {
                var filePath = Path.Combine(_environment.WebRootPath, material.FilePath);
                
                if (!System.IO.File.Exists(filePath))
                {
                    _logger.LogWarning($"Fișierul nu a fost găsit: {filePath}");
                    TempData["ErrorMessage"] = "Fișierul nu a fost găsit pe server.";
                    return RedirectToPage("./Details", new { id = material.CourseId });
                }

                var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
                var contentType = material.ContentType ?? "application/octet-stream";

                // Forțează download-ul cu header-uri corecte
                Response.Headers["Content-Disposition"] = $"attachment; filename=\"{material.FileName}\"";
                Response.Headers["Content-Length"] = fileBytes.Length.ToString();

                return File(fileBytes, contentType, material.FileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Eroare la descărcarea fișierului {material.FileName}");
                TempData["ErrorMessage"] = "A apărut o eroare la descărcarea fișierului.";
                return RedirectToPage("./Details", new { id = material.CourseId });
            }
        }
    }
}

