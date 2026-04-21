using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Uniflow.Models;
using Uniflow.Services;

namespace Uniflow.Pages.Vouchers
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly VoucherService _voucherService;
        private readonly GamificationService _gamificationService;
        private readonly UserManager<IdentityUser> _userManager;

        public IndexModel(
            VoucherService voucherService, 
            GamificationService gamificationService,
            UserManager<IdentityUser> userManager)
        {
            _voucherService = voucherService;
            _gamificationService = gamificationService;
            _userManager = userManager;
        }

        public List<UserVoucher> ActiveVouchers { get; set; } = new();
        public List<UserVoucher> ExpiredVouchers { get; set; } = new();
        public List<UserVoucher> RedeemedVouchers { get; set; } = new();
        public int CurrentLevel { get; set; }
        public int CurrentXP { get; set; }

        public async Task OnGetAsync()
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return;

            // Obține toate voucherele utilizatorului
            var allVouchers = await _voucherService.GetUserVouchersAsync(userId);

            var now = DateTime.Now;

            // Categorisire vouchere
            ActiveVouchers = allVouchers
                .Where(v => !v.IsRedeemed && v.ExpiryDate > now)
                .OrderBy(v => v.ExpiryDate)
                .ToList();

            RedeemedVouchers = allVouchers
                .Where(v => v.IsRedeemed)
                .OrderByDescending(v => v.RedeemedDate)
                .ToList();

            ExpiredVouchers = allVouchers
                .Where(v => !v.IsRedeemed && v.ExpiryDate <= now)
                .OrderByDescending(v => v.ExpiryDate)
                .ToList();

            // Obține nivelul curent (pentru info)
            var profile = await _gamificationService.GetUserProfileAsync(userId);
            if (profile != null)
            {
                CurrentXP = profile.XP;
                CurrentLevel = _gamificationService.CalculateLevel(profile.XP);
            }
        }
    }
}
