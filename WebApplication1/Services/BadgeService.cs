using Microsoft.EntityFrameworkCore;
using Uniflow.Data;
using Uniflow.Models;

namespace Uniflow.Services
{
    public class BadgeService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<BadgeService> _logger;

        public BadgeService(ApplicationDbContext context, ILogger<BadgeService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task EnsureBadgesExistAsync()
        {
            // Create tables if not exist (Maunal Migration Workaround)
            try
            {
                var createBadgesTable = @"
                    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Badges')
                    BEGIN
                        CREATE TABLE Badges (
                            Id int NOT NULL IDENTITY,
                            Name nvarchar(100) NOT NULL,
                            Description nvarchar(500) NOT NULL,
                            IconClass nvarchar(50) NOT NULL,
                            ColorClass nvarchar(20) NOT NULL,
                            Type nvarchar(50) NOT NULL,
                            CONSTRAINT PK_Badges PRIMARY KEY (Id)
                        );
                    END";
                await _context.Database.ExecuteSqlRawAsync(createBadgesTable);

                var createUserBadgesTable = @"
                    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'UserBadges')
                    BEGIN
                        CREATE TABLE UserBadges (
                            Id int NOT NULL IDENTITY,
                            UserId nvarchar(450) NOT NULL,
                            BadgeId int NOT NULL,
                            EarnedDate datetime2 NOT NULL,
                            CONSTRAINT PK_UserBadges PRIMARY KEY (Id),
                            CONSTRAINT FK_UserBadges_AspNetUsers_UserId FOREIGN KEY (UserId) REFERENCES AspNetUsers (Id) ON DELETE CASCADE,
                            CONSTRAINT FK_UserBadges_Badges_BadgeId FOREIGN KEY (BadgeId) REFERENCES Badges (Id) ON DELETE CASCADE
                        );
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_UserBadges_BadgeId')
                            CREATE INDEX IX_UserBadges_BadgeId ON UserBadges (BadgeId);
                        
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_UserBadges_UserId_BadgeId')
                            CREATE UNIQUE INDEX IX_UserBadges_UserId_BadgeId ON UserBadges (UserId, BadgeId);
                    END";
                await _context.Database.ExecuteSqlRawAsync(createUserBadgesTable);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating badge tables manually.");
            }

            // Define all badges
            var badges = new List<Badge>
            {
                // Rank Badges
                new Badge { Name = "Legendă 🏆", Description = "Poziția #1 în clasamentul general - Un adevărat lider!", IconClass = "/badges/7.png", ColorClass = "warning", Type = "Rank1" },
                new Badge { Name = "Maestru Academic", Description = "Poziția #2 în clasamentul general - Excelență remarcabilă!", IconClass = "/badges/6.png", ColorClass = "secondary", Type = "Rank2" },
                new Badge { Name = "Performer de Elită", Description = "Poziția #3 în clasamentul general - Top 3 studenți!", IconClass = "/badges/5.png", ColorClass = "danger", Type = "Rank3" },

                // Note Achievements
                new Badge { Name = "Conținut Viral 🔥", Description = "Notița ta a inspirat întreaga comunitate - Cele mai multe aprecieri!", IconClass = "/badges/4.png", ColorClass = "danger", Type = "MostLikesReceived" },
                new Badge { Name = "Influencer Academic", Description = "Notița ta a generat cele mai multe discuții și comentarii", IconClass = "/badges/3.png", ColorClass = "info", Type = "MostCommentsReceived" },
                new Badge { Name = "Conținut de Calitate", Description = "Ai creat o notiță apreciată de comunitate (5+ upvotes)", IconClass = "/badges/2.png", ColorClass = "primary", Type = "PopularNote" },

                // User Activity
                new Badge { Name = "Expert Evaluator", Description = "Cel mai activ în evaluarea conținutului - Cele mai multe voturi acordate", IconClass = "/badges/1.png", ColorClass = "dark", Type = "MostActiveVoter" },

                // Validation Achievements
                new Badge { Name = "Contributor Suprem 👑", Description = "#1 în producția de conținut validat - Un adevărat creator!", IconClass = "/badges/10.png", ColorClass = "warning", Type = "TopContributor1" },
                new Badge { Name = "Creator de Excelență", Description = "#2 în producția de conținut validat - Contribuții remarcabile", IconClass = "/badges/9.png", ColorClass = "secondary", Type = "TopContributor2" },
                new Badge { Name = "Scriitor Dedicat", Description = "#3 în producția de conținut validat - Top 3 contributori", IconClass = "/badges/8.png", ColorClass = "info", Type = "TopContributor3" }
            };


            foreach (var badge in badges)
            {
                var existingBadge = await _context.Badges.FirstOrDefaultAsync(b => b.Type == badge.Type);
                if (existingBadge != null)
                {
                    // Update existing badge with new name, description, icon, and color
                    existingBadge.Name = badge.Name;
                    existingBadge.Description = badge.Description;
                    existingBadge.IconClass = badge.IconClass;
                    existingBadge.ColorClass = badge.ColorClass;
                    _context.Badges.Update(existingBadge);
                }
                else
                {
                    // Create new badge if it doesn't exist
                    _context.Badges.Add(badge);
                }
            }
            await _context.SaveChangesAsync();
        }

        public async Task RefreshDynamicBadgesAsync()
        {
            // Clear existing Dynamic badges (Ranks, Most X) to recalculate current winners
            // We do NOT clear "Achievement" badges like PopularNote which are earned permanently (or maybe we do if the criteria is strictly "All notes > 4 likes"? No, usually "Has a note" is permanent).
            // Let's assume PopularNote is permanent.
            // Ranks and "Most" badges are dynamic.
            
            var dynamicTypes = new[] { "Rank1", "Rank2", "Rank3", "MostLikesReceived", "MostCommentsReceived", "MostActiveVoter", "TopContributor1", "TopContributor2", "TopContributor3" };
            
            // Remove old dynamic badges
            var oldBadges = await _context.UserBadges
                .Include(ub => ub.Badge)
                .Where(ub => dynamicTypes.Contains(ub.Badge!.Type))
                .ToListAsync();
            
            _context.UserBadges.RemoveRange(oldBadges);
            await _context.SaveChangesAsync();

            // 1. Assign Ranks (XP)
            var topUsers = await _context.UserProfiles
                .OrderByDescending(p => p.XP)
                .Take(3)
                .Select(p => p.UserId)
                .ToListAsync();

            if (topUsers.Count >= 1) await AwardBadgeByTypeAsync(topUsers[0], "Rank1");
            if (topUsers.Count >= 2) await AwardBadgeByTypeAsync(topUsers[1], "Rank2");
            if (topUsers.Count >= 3) await AwardBadgeByTypeAsync(topUsers[2], "Rank3");

            // 2. Most Likes Received (Note Author)
            // Complex query: Select UserId of note with Max(Upvotes)
            // Simplified: Order notes by Upvotes desc take 1
            // Need to join NoteVotes... or use a cached count if available.
            // We can query Notes and count votes.
            var mostLikedNote = await _context.Notes
                .Select(n => new { n.StudentId, Likes = n.Votes.Count(v => v.IsUpvote) })
                .OrderByDescending(x => x.Likes)
                .FirstOrDefaultAsync();
            
            if (mostLikedNote != null && mostLikedNote.Likes > 0)
            {
                await AwardBadgeByTypeAsync(mostLikedNote.StudentId, "MostLikesReceived");
            }

            // 3. Most Comments Received
            var mostCommentedNote = await _context.Notes
                .Select(n => new { n.StudentId, Comments = n.Comments.Count() })
                .OrderByDescending(x => x.Comments)
                .FirstOrDefaultAsync();

            if (mostCommentedNote != null && mostCommentedNote.Comments > 0)
            {
                await AwardBadgeByTypeAsync(mostCommentedNote.StudentId, "MostCommentsReceived");
            }

            // 4. Most Active Voter
            var topVoter = await _context.NoteVotes
                .GroupBy(v => v.UserId)
                .Select(g => new { UserId = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .FirstOrDefaultAsync();

            if (topVoter != null && topVoter.Count > 0)
            {
                await AwardBadgeByTypeAsync(topVoter.UserId, "MostActiveVoter");
            }

            // 5. Top Contributors (Validated Notes)
            // Assuming Status 'Validated' exists. Or we check logic.
            // Assuming string status for now based on context.
            var topContributors = await _context.Notes
                .Where(n => n.Status == "Validated" || n.Status == "Approved") // Check literal
                .GroupBy(n => n.StudentId)
                .Select(g => new { UserId = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(3)
                .ToListAsync();

            if (topContributors.Count >= 1) await AwardBadgeByTypeAsync(topContributors[0].UserId, "TopContributor1");
            if (topContributors.Count >= 2) await AwardBadgeByTypeAsync(topContributors[1].UserId, "TopContributor2");
            if (topContributors.Count >= 3) await AwardBadgeByTypeAsync(topContributors[2].UserId, "TopContributor3");
        }

        public async Task CheckPersistentBadgesAsync(string userId)
        {
            // Check "PopularNote" (> 4 likes)
            // Does user have ANY note with > 4 likes?
            var hasPopularNote = await _context.Notes
                .AnyAsync(n => n.StudentId == userId && n.Votes.Count(v => v.IsUpvote) > 4);

            if (hasPopularNote)
            {
                await AwardBadgeByTypeAsync(userId, "PopularNote");
            }
        }

        private async Task AwardBadgeByTypeAsync(string userId, string badgeType)
        {
            var badge = await _context.Badges.FirstOrDefaultAsync(b => b.Type == badgeType);
            if (badge == null) return;

            // Check if already has it (for persistent ones, dynamic ones are cleared so simple add is safe usually but let's be safe)
            if (!await _context.UserBadges.AnyAsync(ub => ub.UserId == userId && ub.BadgeId == badge.Id))
            {
                _context.UserBadges.Add(new UserBadge
                {
                    UserId = userId,
                    BadgeId = badge.Id,
                    EarnedDate = DateTime.Now
                });
                await _context.SaveChangesAsync();
            }
        }
    }
}
