using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Uniflow.Data;
using Uniflow.Models;

namespace Uniflow.Pages.Notifications
{
    [Authorize]
    public class FeedModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IAntiforgery _antiforgery;

        public FeedModel(ApplicationDbContext context, UserManager<IdentityUser> userManager, IAntiforgery antiforgery)
        {
            _context = context;
            _userManager = userManager;
            _antiforgery = antiforgery;
        }

        public IList<UserNotification> Notifications { get; set; }

        public async Task<IActionResult> OnGetAsync(int? take = null)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null)
            {
                if (take.HasValue) return new JsonResult(new { unreadCount = 0, items = Array.Empty<object>() });
                return RedirectToPage("/Account/Login", new { area = "Identity" });
            }

            // If "take" is specified, it's an API call for the dropdown
            if (take.HasValue)
            {
                var safeTake = Math.Clamp(take.Value, 1, 50);

                var unreadCount = await _context.UserNotifications
                    .Where(n => n.UserId == userId && !n.IsRead)
                    .CountAsync();

                var items = await _context.UserNotifications
                    .Where(n => n.UserId == userId)
                    .OrderByDescending(n => n.CreatedDate)
                    .Take(safeTake)
                    .Select(n => new
                    {
                        n.Id,
                        n.Title,
                        n.Message,
                        n.Type,
                        n.IsRead,
                        createdDate = n.CreatedDate,
                        n.LinkUrl
                    })
                    .ToListAsync();

                return new JsonResult(new { unreadCount, items });
            }

            // Otherwise, return the full page view
            Notifications = await _context.UserNotifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedDate)
                .Take(100) // Limit to last 100 for the view for now
                .ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostMarkReadAsync(int id)
        {
            await _antiforgery.ValidateRequestAsync(HttpContext);

            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            var n = await _context.UserNotifications.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);
            if (n == null) return NotFound();

            if (!n.IsRead)
            {
                n.IsRead = true;
                await _context.SaveChangesAsync();
            }

            return new JsonResult(new { success = true });
        }

        public async Task<IActionResult> OnPostMarkAllReadAsync()
        {
            await _antiforgery.ValidateRequestAsync(HttpContext);

            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            await _context.UserNotifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsRead, true));

            return new JsonResult(new { success = true });
        }
    }
}

