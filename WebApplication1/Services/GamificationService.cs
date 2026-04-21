using Microsoft.EntityFrameworkCore;
using Uniflow.Data;
using Uniflow.Models;

namespace Uniflow.Services
{
    /// <summary>
    /// Serviciu pentru gestionarea sistemului de gamificare (XP - Experience Points)
    /// </summary>
    public class GamificationService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<GamificationService> _logger;
        private readonly VoucherService _voucherService;
        private readonly NotificationService _notificationService;

        // Valori XP pentru diferite acțiuni
        public const int XP_ENROLL_COURSE = 50;
        public const int XP_NOTE_APPROVED = 30; // XP pentru aprobare notiță
        
        // Tier-based XP rewards pentru upvotes (sistem progresiv)
        private const int TIER1_UPVOTE_XP = 10;  // Primii 10 upvotes
        private const int TIER2_UPVOTE_XP = 8;   // Upvotes 11-25
        private const int TIER3_UPVOTE_XP = 5;   // Upvotes 26-50
        private const int TIER4_UPVOTE_XP = 3;   // Upvotes 51-100
        private const int TIER5_UPVOTE_XP = 2;   // Upvotes 100+

        // Downvote penalties (minimale pentru a nu descuraja)
        private const int TIER1_DOWNVOTE_PENALTY = 2;  // Primii 5 downvotes
        private const int TIER2_DOWNVOTE_PENALTY = 3;  // Downvotes 6-15
        private const int TIER3_DOWNVOTE_PENALTY = 1;  // Downvotes 15+

        // Milestone bonuses (extra reward pentru popularitate)
        public const int MILESTONE_10_BONUS = 25;
        public const int MILESTONE_25_BONUS = 50;
        public const int MILESTONE_50_BONUS = 100;
        public const int MILESTONE_100_BONUS = 200;

        public GamificationService(
            ApplicationDbContext context, 
            ILogger<GamificationService> logger,
            VoucherService voucherService,
            NotificationService notificationService)
        {
            _context = context;
            _logger = logger;
            _voucherService = voucherService;
            _notificationService = notificationService;
        }

        /// <summary>
        /// Acordă XP unui utilizator pentru o acțiune specifică
        /// </summary>
        public async Task AwardXPAsync(string userId, int xpAmount, string reason)
        {
            try
            {
                // Check if user has Student role
                var user = await _context.Users.FindAsync(userId);
                if (user == null) return;

                var isStudent = await (from ur in _context.UserRoles
                                     join r in _context.Roles on ur.RoleId equals r.Id
                                     where ur.UserId == userId && r.Name == "Student"
                                     select r).AnyAsync();

                if (!isStudent)
                {
                    _logger.LogInformation($"XP denied for user {userId} (Role is not Student).");
                    return;
                }

                // Găsim sau creăm profilul utilizatorului
                var profile = await _context.UserProfiles
                    .FirstOrDefaultAsync(p => p.UserId == userId);

                if (profile == null)
                {
                    // Creăm profilul dacă nu există
                    profile = new UserProfile
                    {
                        UserId = userId,
                        FirstName = "",
                        LastName = "",
                        XP = 0
                    };
                    _context.UserProfiles.Add(profile);
                }

                // Calculăm nivelul ÎNAINTE și DUPĂ adăugarea XP
                int oldLevel = CalculateLevel(profile.XP);
                
                // Adăugăm XP (nu permitem XP negativ)
                profile.XP = Math.Max(0, profile.XP + xpAmount);
                int newLevel = CalculateLevel(profile.XP);

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Acordat {xpAmount} XP utilizatorului {userId} pentru: {reason}. Total XP: {profile.XP}");

                // Verificăm dacă a avut loc un level-up și acordăm voucher dacă e necesar
                await CheckAndAwardVoucherForLevelUpAsync(userId, oldLevel, newLevel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Eroare la acordarea XP utilizatorului {userId}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Obține XP-ul unui utilizator
        /// </summary>
        public async Task<int> GetXPAsync(string userId)
        {
            try
            {
                var profile = await _context.UserProfiles
                    .FirstOrDefaultAsync(p => p.UserId == userId);

                return profile?.XP ?? 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Eroare la obținerea XP pentru utilizator {userId}. Tabelul UserProfiles poate să nu existe încă.");
                return 0;
            }
        }

        /// <summary>
        /// Obține clasamentul utilizatorilor (leaderboard) ordonat după XP
        /// </summary>
        public async Task<List<LeaderboardEntry>> GetLeaderboardAsync(int topCount = 10)
        {
            try
            {
                // Join with UserRoles and Roles to filter only Students
                var query = from p in _context.UserProfiles
                            join u in _context.Users on p.UserId equals u.Id
                            join ur in _context.UserRoles on u.Id equals ur.UserId
                            join r in _context.Roles on ur.RoleId equals r.Id
                            where r.Name == "Student"
                            orderby p.XP descending
                            select new LeaderboardEntry
                            {
                                UserId = p.UserId,
                                UserEmail = u.Email ?? "N/A",
                                UserName = (p.FirstName == "" && p.LastName == "") ? u.UserName : (p.FirstName + " " + p.LastName),
                                XP = p.XP
                            };

                return await query.Take(topCount).ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Eroare la obținerea clasamentului. Tabelul UserProfiles poate să nu există încă.");
                // Returnează lista goală dacă tabelul nu există
                return new List<LeaderboardEntry>();
            }
        }

        /// <summary>
        /// Obține poziția unui utilizator în clasament
        /// </summary>
        public async Task<int> GetUserRankAsync(string userId)
        {
            try
            {
                var userXP = await GetXPAsync(userId);
                // Count users with 'Student' role who have more XP
                var usersAbove = await (from p in _context.UserProfiles
                                        join u in _context.Users on p.UserId equals u.Id
                                        join ur in _context.UserRoles on u.Id equals ur.UserId
                                        join r in _context.Roles on ur.RoleId equals r.Id
                                        where r.Name == "Student" && p.XP > userXP
                                        select p).CountAsync();

                return usersAbove + 1; // Poziția (1-indexed)
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Eroare la obținerea rank-ului pentru utilizator {userId}.");
                return 0;
            }
        }

        /// <summary>
        /// Calculează XP-ul pentru upvotes folosind sistem tier-based (progresiv descrescător)
        /// Primii 10 upvotes = 10 XP fiecare, 11-25 = 8 XP, etc.
        /// </summary>
        private int CalculateUpvoteXP(int upvoteCount)
        {
            int totalXP = 0;

            // Tier 1: Primii 10 upvotes (1-10)
            int tier1Count = Math.Min(upvoteCount, 10);
            totalXP += tier1Count * TIER1_UPVOTE_XP;

            if (upvoteCount <= 10) return totalXP;

            // Tier 2: Upvotes 11-25
            int tier2Count = Math.Min(upvoteCount - 10, 15);
            totalXP += tier2Count * TIER2_UPVOTE_XP;

            if (upvoteCount <= 25) return totalXP;

            // Tier 3: Upvotes 26-50
            int tier3Count = Math.Min(upvoteCount - 25, 25);
            totalXP += tier3Count * TIER3_UPVOTE_XP;

            if (upvoteCount <= 50) return totalXP;

            // Tier 4: Upvotes 51-100
            int tier4Count = Math.Min(upvoteCount - 50, 50);
            totalXP += tier4Count * TIER4_UPVOTE_XP;

            if (upvoteCount <= 100) return totalXP;

            // Tier 5: Upvotes 100+
            int tier5Count = upvoteCount - 100;
            totalXP += tier5Count * TIER5_UPVOTE_XP;

            return totalXP;
        }

        /// <summary>
        /// Calculează penalizarea pentru downvotes (tier-based, crescătoare)
        /// </summary>
        private int CalculateDownvotePenalty(int downvoteCount)
        {
            int totalPenalty = 0;

            // Tier 1: Primii 5 downvotes
            int tier1Count = Math.Min(downvoteCount, 5);
            totalPenalty += tier1Count * TIER1_DOWNVOTE_PENALTY;

            if (downvoteCount <= 5) return totalPenalty;

            // Tier 2: Downvotes 6-15
            int tier2Count = Math.Min(downvoteCount - 5, 10);
            totalPenalty += tier2Count * TIER2_DOWNVOTE_PENALTY;

            if (downvoteCount <= 15) return totalPenalty;

            // Tier 3: Downvotes 15+
            int tier3Count = downvoteCount - 15;
            totalPenalty += tier3Count * TIER3_DOWNVOTE_PENALTY;

            return totalPenalty;
        }

        /// <summary>
        /// Calculează bonusurile pentru milestone-uri atinse
        /// </summary>
        private int CalculateMilestoneBonuses(int upvoteCount)
        {
            int bonuses = 0;

            if (upvoteCount >= 100) bonuses += MILESTONE_100_BONUS;
            if (upvoteCount >= 50) bonuses += MILESTONE_50_BONUS;
            if (upvoteCount >= 25) bonuses += MILESTONE_25_BONUS;
            if (upvoteCount >= 10) bonuses += MILESTONE_10_BONUS;

            return bonuses;
        }

        /// <summary>
        /// Calculează XP-ul total pentru o notiță bazat pe upvote-uri și downvote-uri
        /// Formula nouă: tier-based + milestone bonuses - minimal downvote penalty
        /// </summary>
        public int CalculateXPFromVotes(int upvotes, int downvotes)
        {
            // Calculăm XP din upvotes (tier-based)
            int upvoteXP = CalculateUpvoteXP(upvotes);

            // Calculăm penalizarea din downvotes (tier-based)
            int downvotePenalty = CalculateDownvotePenalty(downvotes);

            // Calculăm bonusuri milestone
            int milestoneBonuses = CalculateMilestoneBonuses(upvotes);

            // XP total = upvotes - downvotes + milestones
            int totalXP = upvoteXP - downvotePenalty + milestoneBonuses;

            // Protecție: XP din voturi nu poate fi negativ
            return Math.Max(0, totalXP);
        }

        /// <summary>
        /// Acordă XP autorului notiței bazat pe voturile primite
        /// Această metodă ar trebui apelată când notița este validată sau când primește voturi noi
        /// </summary>
        public async Task AwardXPFromNoteVotesAsync(int noteId, string authorUserId, string noteTitle)
        {
            try
            {
                // Obține notița cu XP-ul deja acordat
                var note = await _context.Notes
                    .FirstOrDefaultAsync(n => n.Id == noteId);

                if (note == null) return;

                // Obține numărul de upvote-uri și downvote-uri pentru notiță
                var upvotes = await _context.NoteVotes
                    .CountAsync(v => v.NoteId == noteId && v.IsUpvote);
                
                var downvotes = await _context.NoteVotes
                    .CountAsync(v => v.NoteId == noteId && !v.IsUpvote);

                // Calculează XP-ul total bazat pe formulă
                var totalXPForVotes = CalculateXPFromVotes(upvotes, downvotes);

                // Calculează diferența față de XP-ul deja acordat
                var xpToAward = totalXPForVotes - note.XPAwardedForVotes;

                // Acordăm/scădem XP dacă există diferență (pozitivă sau negativă)
                if (xpToAward != 0)
                {
                    var action = xpToAward > 0 ? "primit" : "pierdut";
                    var absXP = Math.Abs(xpToAward);
                    
                    await AwardXPAsync(authorUserId, xpToAward, 
                        $"Notiță votată: {noteTitle} ({upvotes} upvotes, {downvotes} downvotes) - {action} {absXP} XP");
                    
                    // Actualizează XP-ul acordat pentru voturi
                    note.XPAwardedForVotes = totalXPForVotes;
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Eroare la calcularea XP pentru notița {noteId}");
            }
        }

        /// <summary>
        /// Obține profilul unui utilizator
        /// </summary>
        public async Task<UserProfile?> GetUserProfileAsync(string userId)
        {
            return await _context.UserProfiles
                .FirstOrDefaultAsync(p => p.UserId == userId);
        }

        /// <summary>
        /// Calculează nivelul pe bază de XP (formula: level = xp / 100)
        /// </summary>
        public int CalculateLevel(int xp)
        {
            return xp / 100;
        }

        /// <summary>
        /// Verifică dacă utilizatorul a atins un nivel milestone (5, 10, 15, etc.) și acordă voucher
        /// </summary>
        private async Task CheckAndAwardVoucherForLevelUpAsync(string userId, int oldLevel, int newLevel)
        {
            if (newLevel <= oldLevel) return; // Nu a fost level-up

            // Verificăm fiecare nivel atins între oldLevel și newLevel
            for (int level = oldLevel + 1; level <= newLevel; level++)
            {
                // Verificăm dacă e nivel milestone (multiplu de 5)
                if (level % 5 == 0)
                {
                    try
                    {
                        // Verificăm dacă există voucher pentru acest nivel
                        var voucher = await _voucherService.GetAvailableVoucherForLevelAsync(level);
                        if (voucher == null) continue;

                        // Verificăm dacă utilizatorul a primit deja acest voucher
                        bool alreadyReceived = await _voucherService.HasReceivedVoucherForLevelAsync(userId, level);
                        if (alreadyReceived) continue;

                        // Acordăm voucherul
                        var userVoucher = await _voucherService.AwardVoucherAsync(userId, voucher.Id);
                        
                        if (userVoucher != null)
                        {
                            // Trimitem notificare
                            await _notificationService.NotifyVoucherAwardedAsync(
                                userId, 
                                voucher.Title, 
                                voucher.PartnerName,
                                userVoucher.Code,
                                level
                            );

                            _logger.LogInformation($"Awarded level {level} voucher '{voucher.Title}' to user {userId}.");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error awarding voucher for level {level} to user {userId}.");
                    }
                }
            }
        }

        public class LeaderboardEntry
        {
            public string UserId { get; set; } = string.Empty;
            public string UserEmail { get; set; } = string.Empty;
            public string UserName { get; set; } = string.Empty;
            public int XP { get; set; }
        }
    }
}

