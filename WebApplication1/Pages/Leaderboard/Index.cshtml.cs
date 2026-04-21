using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Uniflow.Services;

namespace Uniflow.Pages.Leaderboard
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly GamificationService _gamificationService;

        public IndexModel(GamificationService gamificationService)
        {
            _gamificationService = gamificationService;
        }

        public List<GamificationService.LeaderboardEntry> Leaderboard { get; set; } = new List<GamificationService.LeaderboardEntry>();

        public async Task OnGetAsync()
        {
            Leaderboard = await _gamificationService.GetLeaderboardAsync(50); // Top 50
        }
    }
}




