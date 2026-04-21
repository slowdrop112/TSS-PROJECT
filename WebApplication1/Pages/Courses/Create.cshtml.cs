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
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public CreateModel(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public IActionResult OnGet()
        {
            return Page();
        }

        [BindProperty]
        public Course Course { get; set; } = default!;

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Setăm profesorul ca fiind utilizatorul curent autentificat
            var userId = _userManager.GetUserId(User);
            if (userId == null)
            {
                return Forbid();
            }
            
            // Creăm cursul și setăm explicit toate proprietățile
            var newCourse = new Course
            {
                Title = Course.Title,
                Description = Course.Description,
                Category = Course.Category,
                DurationHours = Course.DurationHours,
                ProfesorId = userId,  // IMPORTANT: Setăm ProfesorId
                CreatedDate = DateTime.Now,
                IsPublished = true
            };

            _context.Courses.Add(newCourse);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}
