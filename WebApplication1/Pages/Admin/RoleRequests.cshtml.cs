using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Uniflow.Data;
using Uniflow.Models;

namespace Uniflow.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class RoleRequestsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public RoleRequestsModel(ApplicationDbContext context, UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public List<RoleRequestViewModel> PendingRequests { get; set; } = new List<RoleRequestViewModel>();

        public async Task OnGetAsync()
        {
            var pendingRequests = await _context.RoleRequests
                .Where(r => r.Status == "Pending")
                .Include(r => r.User)
                .OrderBy(r => r.RequestDate)
                .ToListAsync();

            foreach (var request in pendingRequests)
            {
                PendingRequests.Add(new RoleRequestViewModel
                {
                    Id = request.Id,
                    UserEmail = request.User?.Email ?? "Necunoscut",
                    RequestedRole = request.RequestedRole,
                    RequestDate = request.RequestDate
                });
            }
        }

        public async Task<IActionResult> OnPostApproveAsync(int requestId)
        {
            var request = await _context.RoleRequests
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Id == requestId);

            if (request == null || request.User == null)
            {
                return NotFound();
            }

            // Verificăm dacă rolul există
            if (!await _roleManager.RoleExistsAsync(request.RequestedRole))
            {
                await _roleManager.CreateAsync(new IdentityRole(request.RequestedRole));
            }

            // Ștergem rolul Student dacă există (pentru că profesorii nu ar trebui să aibă rol de Student)
            var userRoles = await _userManager.GetRolesAsync(request.User);
            if (userRoles.Contains("Student"))
            {
                await _userManager.RemoveFromRoleAsync(request.User, "Student");
            }

            // Adăugăm rolul Profesor utilizatorului
            await _userManager.AddToRoleAsync(request.User, request.RequestedRole);

            // Marchează cererea ca aprobată
            request.Status = "Approved";
            request.ProcessedDate = DateTime.Now;
            request.ProcessedByUserId = _userManager.GetUserId(User);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Rolul {request.RequestedRole} a fost acordat utilizatorului {request.User.Email}.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostRejectAsync(int requestId)
        {
            var request = await _context.RoleRequests
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Id == requestId);

            if (request == null)
            {
                return NotFound();
            }

            // Marchează cererea ca respinsă
            request.Status = "Rejected";
            request.ProcessedDate = DateTime.Now;
            request.ProcessedByUserId = _userManager.GetUserId(User);

            await _context.SaveChangesAsync();

            TempData["InfoMessage"] = $"Cererea a fost respinsă.";
            return RedirectToPage();
        }

        public class RoleRequestViewModel
        {
            public int Id { get; set; }
            public string UserEmail { get; set; } = string.Empty;
            public string RequestedRole { get; set; } = string.Empty;
            public DateTime RequestDate { get; set; }
        }
    }
}

