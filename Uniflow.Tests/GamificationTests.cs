using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Uniflow.Data;
using Uniflow.Models;
using Uniflow.Services;

namespace Uniflow.Tests;

public class GamificationTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly GamificationService _gamificationService;
    private readonly IServiceProvider _serviceProvider;

    public GamificationTests()
    {
        var services = new ServiceCollection();
        
        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseInMemoryDatabase("TestDb_" + Guid.NewGuid().ToString());
        });

        services.AddLogging();

        services.AddIdentity<IdentityUser, IdentityRole>(options =>
        {
            options.Password.RequireDigit = false;
            options.Password.RequiredLength = 6;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = false;
            options.Password.RequireLowercase = false;
            options.User.RequireUniqueEmail = true;
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

        _serviceProvider = services.BuildServiceProvider();
        
        var scope = _serviceProvider.CreateScope();
        _context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        _userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        _roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<GamificationService>>();
        
        var voucherLogger = scope.ServiceProvider.GetRequiredService<ILogger<VoucherService>>();
        var notificationLogger = scope.ServiceProvider.GetRequiredService<ILogger<NotificationService>>();
        
        var voucherService = new VoucherService(_context, voucherLogger);
        var notificationService = new NotificationService(_context, notificationLogger);
        
        _gamificationService = new GamificationService(_context, logger, voucherService, notificationService);
        
        _context.Database.EnsureCreated();
        
        CreateRolesAsync().Wait();
    }

    private async Task CreateRolesAsync()
    {
        string[] roleNames = { "Admin", "Profesor", "Student" };
        foreach (var roleName in roleNames)
        {
            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                await _roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }
    }

    [Fact]
    public async Task AwardXP_Utilizator_Fara_Profil_Creeaza_Profil_Si_Acorda_XP()
    {
        var user = new IdentityUser { UserName = "test@test.com", Email = "test@test.com", EmailConfirmed = true };
        await _userManager.CreateAsync(user, "Test123!");
        await _userManager.AddToRoleAsync(user, "Student");

        await _gamificationService.AwardXPAsync(user.Id, 50, "Test XP");

        var profile = await _context.UserProfiles.FindAsync(user.Id);
        Assert.NotNull(profile);
        Assert.Equal(50, profile.XP);
    }

    [Fact]
    public async Task AwardXP_Utilizator_Cu_Profil_Adauga_XP_La_Total()
    {
        var user = new IdentityUser { UserName = "test@test.com", Email = "test@test.com", EmailConfirmed = true };
        await _userManager.CreateAsync(user, "Test123!");
        await _userManager.AddToRoleAsync(user, "Student");

        var profile = new UserProfile
        {
            UserId = user.Id,
            FirstName = "Test",
            LastName = "User",
            XP = 100
        };
        _context.UserProfiles.Add(profile);
        await _context.SaveChangesAsync();

        await _gamificationService.AwardXPAsync(user.Id, 50, "Test XP");

        var updatedProfile = await _context.UserProfiles.FindAsync(user.Id);
        Assert.NotNull(updatedProfile);
        Assert.Equal(150, updatedProfile.XP);
    }

    [Fact]
    public async Task AwardXP_XP_Negativ_Nu_Face_XP_Sub_Zero()
    {
        var user = new IdentityUser { UserName = "test@test.com", Email = "test@test.com", EmailConfirmed = true };
        await _userManager.CreateAsync(user, "Test123!");
        await _userManager.AddToRoleAsync(user, "Student");

        var profile = new UserProfile
        {
            UserId = user.Id,
            FirstName = "Test",
            LastName = "User",
            XP = 20
        };
        _context.UserProfiles.Add(profile);
        await _context.SaveChangesAsync();

        await _gamificationService.AwardXPAsync(user.Id, -50, "Downvote");

        var updatedProfile = await _context.UserProfiles.FindAsync(user.Id);
        Assert.NotNull(updatedProfile);
        Assert.Equal(0, updatedProfile.XP);
    }

    [Fact]
    public async Task GetXP_Utilizator_Fara_Profil_Returneaza_Zero()
    {
        var user = new IdentityUser { UserName = "test@test.com", Email = "test@test.com", EmailConfirmed = true };
        await _userManager.CreateAsync(user, "Test123!");
        await _userManager.AddToRoleAsync(user, "Student");

        var xp = await _gamificationService.GetXPAsync(user.Id);

        Assert.Equal(0, xp);
    }

    [Fact]
    public async Task GetXP_Utilizator_Cu_Profil_Returneaza_XP_Corect()
    {
        var user = new IdentityUser { UserName = "test@test.com", Email = "test@test.com", EmailConfirmed = true };
        await _userManager.CreateAsync(user, "Test123!");
        await _userManager.AddToRoleAsync(user, "Student");

        var profile = new UserProfile
        {
            UserId = user.Id,
            FirstName = "Test",
            LastName = "User",
            XP = 150
        };
        _context.UserProfiles.Add(profile);
        await _context.SaveChangesAsync();

        var xp = await _gamificationService.GetXPAsync(user.Id);

        Assert.Equal(150, xp);
    }

    [Fact]
    public async Task GetLeaderboard_Ordoneaza_Utilizatorii_Dupa_XP_Descrescator()
    {
        var user1 = new IdentityUser { UserName = "user1@test.com", Email = "user1@test.com", EmailConfirmed = true };
        var user2 = new IdentityUser { UserName = "user2@test.com", Email = "user2@test.com", EmailConfirmed = true };
        var user3 = new IdentityUser { UserName = "user3@test.com", Email = "user3@test.com", EmailConfirmed = true };

        await _userManager.CreateAsync(user1, "Test123!");
        await _userManager.CreateAsync(user2, "Test123!");
        await _userManager.CreateAsync(user3, "Test123!");

        await _userManager.AddToRoleAsync(user1, "Student");
        await _userManager.AddToRoleAsync(user2, "Student");
        await _userManager.AddToRoleAsync(user3, "Student");

        _context.UserProfiles.Add(new UserProfile { UserId = user1.Id, FirstName = "User1", LastName = "Test", XP = 100 });
        _context.UserProfiles.Add(new UserProfile { UserId = user2.Id, FirstName = "User2", LastName = "Test", XP = 200 });
        _context.UserProfiles.Add(new UserProfile { UserId = user3.Id, FirstName = "User3", LastName = "Test", XP = 50 });
        await _context.SaveChangesAsync();

        var leaderboard = await _gamificationService.GetLeaderboardAsync(10);

        Assert.Equal(3, leaderboard.Count);
        Assert.Equal(200, leaderboard[0].XP);
        Assert.Equal(100, leaderboard[1].XP);
        Assert.Equal(50, leaderboard[2].XP);
    }

    [Fact]
    public async Task GetLeaderboard_Limiteaza_Numarul_De_Rezultate()
    {
        var users = new List<IdentityUser>();
        for (int i = 1; i <= 10; i++)
        {
            var user = new IdentityUser { UserName = $"user{i}@test.com", Email = $"user{i}@test.com", EmailConfirmed = true };
            await _userManager.CreateAsync(user, "Test123!");
            await _userManager.AddToRoleAsync(user, "Student");

            _context.UserProfiles.Add(new UserProfile
            {
                UserId = user.Id,
                FirstName = $"User{i}", 
                LastName = "Test",
                XP = i * 10
            });
        }
        await _context.SaveChangesAsync();

        var leaderboard = await _gamificationService.GetLeaderboardAsync(5);

        Assert.Equal(5, leaderboard.Count);
    }

    [Fact]
    public async Task GetUserRank_Calculeaza_Corect_Pozitia_In_Clasament()
    {
        var users = new List<IdentityUser>();
        for (int i = 1; i <= 5; i++)
        {
            var user = new IdentityUser { UserName = $"user{i}@test.com", Email = $"user{i}@test.com", EmailConfirmed = true };
            await _userManager.CreateAsync(user, "Test123!");
            await _userManager.AddToRoleAsync(user, "Student");

            _context.UserProfiles.Add(new UserProfile
            {
                UserId = user.Id,
                FirstName = $"User{i}",
                LastName = "Test",
                XP = i * 50
            });
            users.Add(user);
        }
        await _context.SaveChangesAsync();

        var rank1 = await _gamificationService.GetUserRankAsync(users[4].Id);
        var rank2 = await _gamificationService.GetUserRankAsync(users[3].Id);
        var rank3 = await _gamificationService.GetUserRankAsync(users[2].Id);

        Assert.Equal(1, rank1);
        Assert.Equal(2, rank2);
        Assert.Equal(3, rank3);
    }

    [Fact]
    public async Task AwardXP_La_Inscriere_Curs_Acorda_XP_Corect()
    {
        var profesor = new IdentityUser { UserName = "profesor@test.com", Email = "profesor@test.com", EmailConfirmed = true };
        await _userManager.CreateAsync(profesor, "Test123!");
        await _userManager.AddToRoleAsync(profesor, "Profesor");

        var course = new Course
        {
            Title = "Test Course",
            Description = "Test",
            Category = profesor.Email,
            CreatedDate = DateTime.Now,
            IsPublished = true
        };
        _context.Courses.Add(course);
        await _context.SaveChangesAsync();

        var student = new IdentityUser { UserName = "student@test.com", Email = "student@test.com", EmailConfirmed = true };
        await _userManager.CreateAsync(student, "Test123!");
        await _userManager.AddToRoleAsync(student, "Student");

        await _gamificationService.AwardXPAsync(
            student.Id,
            GamificationService.XP_ENROLL_COURSE,
            $"Înscriere la curs: {course.Title}");

        var xp = await _gamificationService.GetXPAsync(student.Id);
        Assert.Equal(GamificationService.XP_ENROLL_COURSE, xp);
        Assert.Equal(50, xp);
    }

    [Fact]
    public async Task Leaderboard_Utilizatori_Fara_Profil_Nu_Apar_In_Clasament()
    {
        var user1 = new IdentityUser { UserName = "user1@test.com", Email = "user1@test.com", EmailConfirmed = true };
        var user2 = new IdentityUser { UserName = "user2@test.com", Email = "user2@test.com", EmailConfirmed = true };

        await _userManager.CreateAsync(user1, "Test123!");
        await _userManager.CreateAsync(user2, "Test123!");

        await _userManager.AddToRoleAsync(user1, "Student");
        await _userManager.AddToRoleAsync(user2, "Student");

        _context.UserProfiles.Add(new UserProfile { UserId = user1.Id, FirstName = "User1", LastName = "Test", XP = 100 });
        await _context.SaveChangesAsync();

        var leaderboard = await _gamificationService.GetLeaderboardAsync(10);

        Assert.Single(leaderboard);
        Assert.Equal(user1.Id, leaderboard[0].UserId);
    }

    #region EP Upvotes

    [Theory]
    [InlineData(-5, 0, 0)]
    [InlineData(0, 0, 0)]
    [InlineData(5, 0, 50)]
    [InlineData(15, 0, 165)]
    public void EP_Upvotes(int upvotes, int downvotes, int expectedXp)
    {
        int resultXp = _gamificationService.CalculateXPFromVotes(upvotes, downvotes);
        Assert.Equal(expectedXp, resultXp);
    }

    #endregion

    #region EP Downvotes

    [Theory]
    [InlineData(30, 3, 314)]
    [InlineData(60, 10, 525)]
    [InlineData(110, 20, 845)]
    public void EP_Downvotes(int upvotes, int downvotes, int expectedXp)
    {
        int resultXp = _gamificationService.CalculateXPFromVotes(upvotes, downvotes);
        Assert.Equal(expectedXp, resultXp);
    }

    #endregion

    #region BVA Upvotes

    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(1, 0, 10)]
    [InlineData(9, 0, 90)]
    [InlineData(10, 0, 125)]
    [InlineData(11, 0, 133)]
    [InlineData(24, 0, 237)]
    [InlineData(25, 0, 295)]
    [InlineData(26, 0, 300)]
    [InlineData(49, 0, 415)]
    [InlineData(50, 0, 520)]
    [InlineData(51, 0, 523)]
    [InlineData(99, 0, 667)]
    [InlineData(100, 0, 870)]
    [InlineData(101, 0, 872)]
    public void BVA_Upvotes(int upvotes, int downvotes, int expectedXp)
    {
        int resultXp = _gamificationService.CalculateXPFromVotes(upvotes, downvotes);
        Assert.Equal(expectedXp, resultXp);
    }

    #endregion
    #region BVA Downvotes

    [Theory]
    [InlineData(10, -2, 129)]
    [InlineData(10, 0, 125)]
    [InlineData(10, 1, 123)]
    [InlineData(10, 4, 117)]
    [InlineData(10, 5, 115)]
    [InlineData(10, 6, 112)]
    [InlineData(10, 14, 88)]
    [InlineData(10, 15, 85)]
    [InlineData(10, 16, 84)]
    [InlineData(5, 50, 0)]
    public void BVA_Downvotes(int upvotes, int downvotes, int expectedXp)
    {
        int resultXp = _gamificationService.CalculateXPFromVotes(upvotes, downvotes);
        Assert.Equal(expectedXp, resultXp);
    }

    #endregion

    #region BVA Intersection (Frontiere Simultane U + D)

    [Theory]
    [InlineData(10, 5, 115)]
    [InlineData(10, 15, 85)]
    [InlineData(25, 5, 285)]
    [InlineData(25, 15, 255)]
    [InlineData(50, 5, 510)]
    [InlineData(100, 15, 830)]
    public void BVA_Intersection(int upvotes, int downvotes, int expectedXp)
    {
        int resultXp = _gamificationService.CalculateXPFromVotes(upvotes, downvotes);
        Assert.Equal(expectedXp, resultXp);
    }

    #endregion

    #region Mutation Killing Tests

    [Fact]
    public void Kill_Mutant_CalculateLevel_Division()
    {
        int level = _gamificationService.CalculateLevel(200);
        Assert.Equal(2, level); 
    }

    [Fact]
    public async Task Kill_Mutant_AwardXP_Student_Role_Check()
    {
        var user = new IdentityUser { UserName = "profesor_test@test.com", Email = "profesor_test@test.com", EmailConfirmed = true };
        await _userManager.CreateAsync(user, "Test123!");
        await _userManager.AddToRoleAsync(user, "Profesor");

        await _gamificationService.AwardXPAsync(user.Id, 100, "Profezorii nu primesc XP");

        var profile = await _context.UserProfiles.FindAsync(user.Id);
        if (profile != null)
        {
            Assert.Equal(0, profile.XP);
        }
    }

    [Fact]
    public async Task AwardXPFromNoteVotes_Calculates_And_Updates_Note_XPAwarded()
    {
        var user = new IdentityUser { UserName = "author@test.com", Email = "author@test.com" };
        await _userManager.CreateAsync(user, "Test123!");
        await _userManager.AddToRoleAsync(user, "Student");

        var note = new Note { Title = "Test Note", Content = "Content", StudentId = user.Id, CourseId = 1, XPAwardedForVotes = 0 };
        _context.Notes.Add(note);
        
        for (int i = 0; i < 10; i++)
        {
            _context.NoteVotes.Add(new NoteVote { NoteId = note.Id, UserId = $"u{i}", IsUpvote = true });
        }
        await _context.SaveChangesAsync();

        await _gamificationService.AwardXPFromNoteVotesAsync(note.Id, user.Id, note.Title);

        var updatedNote = await _context.Notes.FindAsync(note.Id);
        Assert.Equal(125, updatedNote.XPAwardedForVotes);
        
        var profile = await _context.UserProfiles.FindAsync(user.Id);
        Assert.Equal(125, profile.XP);
    }

    [Fact]
    public async Task CheckAndAwardVoucher_Awards_Voucher_On_Level_Milestone()
    {
        var user = new IdentityUser { UserName = "lucky@test.com", Email = "lucky@test.com" };
        await _userManager.CreateAsync(user, "Test123!");
        await _userManager.AddToRoleAsync(user, "Student");
        
        var voucher = new Voucher { Title = "Mega Reducere", PartnerName = "Partner", RequiredLevel = 5, ValidityDays = 30, IsActive = true };
        _context.Vouchers.Add(voucher);
        await _context.SaveChangesAsync();

        var profile = new UserProfile { UserId = user.Id, XP = 450 };
        _context.UserProfiles.Add(profile);
        await _context.SaveChangesAsync();

        await _gamificationService.AwardXPAsync(user.Id, 100, "Level up bonus");

        var userVouchers = await _context.UserVouchers.Where(uv => uv.UserId == user.Id).ToListAsync();
        Assert.Single(userVouchers);
        Assert.Equal(voucher.Id, userVouchers[0].VoucherId);
    }

    [Fact]
    public async Task GetLeaderboard_Handles_Users_With_Empty_Names_Correctly()
    {
        var user = new IdentityUser { UserName = "noname@test.com", Email = "noname@test.com" };
        await _userManager.CreateAsync(user, "Test123!");
        await _userManager.AddToRoleAsync(user, "Student");
        
        _context.UserProfiles.Add(new UserProfile { UserId = user.Id, FirstName = "", LastName = "", XP = 500 });
        await _context.SaveChangesAsync();

        var leaderboard = await _gamificationService.GetLeaderboardAsync();

        var entry = leaderboard.FirstOrDefault(e => e.UserId == user.Id);
        Assert.NotNull(entry);
        Assert.Equal("noname@test.com", entry.UserName);
    }

    #endregion

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        if (_serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}