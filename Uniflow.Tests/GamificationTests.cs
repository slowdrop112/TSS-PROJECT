using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Uniflow.Data;
using Uniflow.Models;
using Uniflow.Services;

namespace Uniflow.Tests;

/// <summary>
/// Teste pentru sistemul de gamificare (XP - Experience Points)
/// SCRUM-38: Testare Automată pentru Sistemul de Gamificare
/// </summary>
public class GamificationTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly GamificationService _gamificationService;
    private readonly IServiceProvider _serviceProvider;

    public GamificationTests()
    {
        // Configurăm serviciile pentru teste
        var services = new ServiceCollection();
        
        // Adăugăm Entity Framework cu InMemory Database
        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseInMemoryDatabase("TestDb_" + Guid.NewGuid().ToString());
        });

        // Adăugăm Logging (necesar pentru Identity și GamificationService)
        services.AddLogging();

        // Adăugăm Identity
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
        
        _gamificationService = new GamificationService(_context, logger, null!, null!);
        
        // Asigurăm că baza de date este creată
        _context.Database.EnsureCreated();
        
        // Creăm rolurile necesare
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
        // Arrange - Creează un utilizator fără profil
        var user = new IdentityUser { UserName = "test@test.com", Email = "test@test.com", EmailConfirmed = true };
        await _userManager.CreateAsync(user, "Test123!");
        await _userManager.AddToRoleAsync(user, "Student");

        // Act - Acordă XP (utilizatorul nu are profil încă)
        await _gamificationService.AwardXPAsync(user.Id, 50, "Test XP");

        // Assert - Verificăm că profilul a fost creat și XP-ul a fost acordat
        var profile = await _context.UserProfiles.FindAsync(user.Id);
        Assert.NotNull(profile);
        Assert.Equal(50, profile.XP);
    }

    [Fact]
    public async Task AwardXP_Utilizator_Cu_Profil_Adauga_XP_La_Total()
    {
        // Arrange - Creează utilizator cu profil
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

        // Act - Acordă XP suplimentar
        await _gamificationService.AwardXPAsync(user.Id, 50, "Test XP");

        // Assert - Verificăm că XP-ul a fost adăugat la totalul existent
        var updatedProfile = await _context.UserProfiles.FindAsync(user.Id);
        Assert.NotNull(updatedProfile);
        Assert.Equal(150, updatedProfile.XP); // 100 + 50
    }

    [Fact]
    public async Task AwardXP_XP_Negativ_Nu_Face_XP_Sub_Zero()
    {
        // Arrange - Creează utilizator cu XP
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

        // Act - Acordă XP negativ (ex: downvote)
        await _gamificationService.AwardXPAsync(user.Id, -50, "Downvote");

        // Assert - Verificăm că XP-ul nu a devenit negativ, ci rămâne 0
        var updatedProfile = await _context.UserProfiles.FindAsync(user.Id);
        Assert.NotNull(updatedProfile);
        Assert.Equal(0, updatedProfile.XP); // Nu poate fi negativ
    }

    [Fact]
    public async Task GetXP_Utilizator_Fara_Profil_Returneaza_Zero()
    {
        // Arrange - Creează utilizator fără profil
        var user = new IdentityUser { UserName = "test@test.com", Email = "test@test.com", EmailConfirmed = true };
        await _userManager.CreateAsync(user, "Test123!");
        await _userManager.AddToRoleAsync(user, "Student");

        // Act - Obține XP
        var xp = await _gamificationService.GetXPAsync(user.Id);

        // Assert - Verificăm că returnează 0
        Assert.Equal(0, xp);
    }

    [Fact]
    public async Task GetXP_Utilizator_Cu_Profil_Returneaza_XP_Corect()
    {
        // Arrange - Creează utilizator cu profil
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

        // Act - Obține XP
        var xp = await _gamificationService.GetXPAsync(user.Id);

        // Assert - Verificăm că returnează XP-ul corect
        Assert.Equal(150, xp);
    }

    [Fact]
    public async Task GetLeaderboard_Ordoneaza_Utilizatorii_Dupa_XP_Descrescator()
    {
        // Arrange - Creează 3 utilizatori cu XP diferit
        var user1 = new IdentityUser { UserName = "user1@test.com", Email = "user1@test.com", EmailConfirmed = true };
        var user2 = new IdentityUser { UserName = "user2@test.com", Email = "user2@test.com", EmailConfirmed = true };
        var user3 = new IdentityUser { UserName = "user3@test.com", Email = "user3@test.com", EmailConfirmed = true };

        await _userManager.CreateAsync(user1, "Test123!");
        await _userManager.CreateAsync(user2, "Test123!");
        await _userManager.CreateAsync(user3, "Test123!");

        await _userManager.AddToRoleAsync(user1, "Student");
        await _userManager.AddToRoleAsync(user2, "Student");
        await _userManager.AddToRoleAsync(user3, "Student");

        // Creăm profiluri cu XP diferit (user2 are cel mai mult, apoi user1, apoi user3)
        _context.UserProfiles.Add(new UserProfile { UserId = user1.Id, FirstName = "User1", LastName = "Test", XP = 100 });
        _context.UserProfiles.Add(new UserProfile { UserId = user2.Id, FirstName = "User2", LastName = "Test", XP = 200 });
        _context.UserProfiles.Add(new UserProfile { UserId = user3.Id, FirstName = "User3", LastName = "Test", XP = 50 });
        await _context.SaveChangesAsync();

        // Act - Obține clasamentul
        var leaderboard = await _gamificationService.GetLeaderboardAsync(10);

        // Assert - Verificăm că sunt ordonați descrescător după XP
        Assert.Equal(3, leaderboard.Count);
        Assert.Equal(200, leaderboard[0].XP); // user2
        Assert.Equal(100, leaderboard[1].XP); // user1
        Assert.Equal(50, leaderboard[2].XP);  // user3
    }

    [Fact]
    public async Task GetLeaderboard_Limiteaza_Numarul_De_Rezultate()
    {
        // Arrange - Creează 10 utilizatori
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

        // Act - Obține doar top 5
        var leaderboard = await _gamificationService.GetLeaderboardAsync(5);

        // Assert - Verificăm că returnează doar 5 utilizatori
        Assert.Equal(5, leaderboard.Count);
    }

    [Fact]
    public async Task GetUserRank_Calculeaza_Corect_Pozitia_In_Clasament()
    {
        // Arrange - Creează 5 utilizatori cu XP diferit
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
                XP = i * 50 // 50, 100, 150, 200, 250
            });
            users.Add(user);
        }
        await _context.SaveChangesAsync();

        // Act & Assert - Verificăm pozițiile
        var rank1 = await _gamificationService.GetUserRankAsync(users[4].Id); // 250 XP - poziția 1
        var rank2 = await _gamificationService.GetUserRankAsync(users[3].Id); // 200 XP - poziția 2
        var rank3 = await _gamificationService.GetUserRankAsync(users[2].Id); // 150 XP - poziția 3

        Assert.Equal(1, rank1);
        Assert.Equal(2, rank2);
        Assert.Equal(3, rank3);
    }

    [Fact]
    public async Task AwardXP_La_Inscriere_Curs_Acorda_XP_Corect()
    {
        // Arrange - Creează profesor și curs
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

        // Arrange - Creează student
        var student = new IdentityUser { UserName = "student@test.com", Email = "student@test.com", EmailConfirmed = true };
        await _userManager.CreateAsync(student, "Test123!");
        await _userManager.AddToRoleAsync(student, "Student");

        // Act - Acordă XP pentru înscriere la curs (simulează logica din aplicație)
        await _gamificationService.AwardXPAsync(
            student.Id,
            GamificationService.XP_ENROLL_COURSE,
            $"Înscriere la curs: {course.Title}");

        // Assert - Verificăm că XP-ul a fost acordat
        var xp = await _gamificationService.GetXPAsync(student.Id);
        Assert.Equal(GamificationService.XP_ENROLL_COURSE, xp);
        Assert.Equal(50, xp); // Verificăm valoarea constantă
    }

    [Fact]
    public async Task Leaderboard_Utilizatori_Fara_Profil_Nu_Apar_In_Clasament()
    {
        // Arrange - Creează utilizator cu profil și utilizator fără profil
        var user1 = new IdentityUser { UserName = "user1@test.com", Email = "user1@test.com", EmailConfirmed = true };
        var user2 = new IdentityUser { UserName = "user2@test.com", Email = "user2@test.com", EmailConfirmed = true };

        await _userManager.CreateAsync(user1, "Test123!");
        await _userManager.CreateAsync(user2, "Test123!");

        await _userManager.AddToRoleAsync(user1, "Student");
        await _userManager.AddToRoleAsync(user2, "Student");

        // Doar user1 are profil
        _context.UserProfiles.Add(new UserProfile { UserId = user1.Id, FirstName = "User1", LastName = "Test", XP = 100 });
        await _context.SaveChangesAsync();

        // Act - Obține clasamentul
        var leaderboard = await _gamificationService.GetLeaderboardAsync(10);

        // Assert - Verificăm că doar user1 apare (user2 nu are profil)
        Assert.Single(leaderboard);
        Assert.Equal(user1.Id, leaderboard[0].UserId);
    }

    #region ETAPA 1 - STUDENT 1: Partitionare in Clase de Echivalenta (Tiers Upvotes)

    [Theory]
    // Partitia 1 (1-9 upvotes): Ex. 5 upvotes.
    // Calcul: 5 upvotes * 10 XP = 50 XP. (Fara milestone). Total = 50.
    [InlineData(5, 0, 50)]
    
    // Partitia 2 (10-24 upvotes): Ex. 15 upvotes.
    // Calcul: 10 * 10 XP + 5 * 8 XP = 140 XP. Milestone 10 (+25). Total = 165.
    [InlineData(15, 0, 165)]

    // Partitia 3 (25-49 upvotes): Ex. 30 upvotes.
    // Calcul: 10*10 + 15*8 + 5*5 = 245 XP. Milestones 10, 25 (+25, +50 = +75). Total = 320.
    [InlineData(30, 0, 320)]

    // Partitia 4 (50-99 upvotes): Ex. 60 upvotes.
    // Calcul: 10*10 + 15*8 + 25*5 + 10*3 = 375 XP. Milestones 10, 25, 50 (+175). Total = 550.
    [InlineData(60, 0, 550)]

    // Partitia 5 (100+ upvotes): Ex. 110 upvotes.
    // Calcul: 10*10 + 15*8 + 25*5 + 50*3 + 10*2 = 515 XP. Toate milestones (+375). Total = 890.
    [InlineData(110, 0, 890)]
    public void CalculateXPFromVotes_EP_ValidUpvoteTiers_ReturnsCorrectXP(int upvotes, int downvotes, int expectedXp)
    {
        // Act
        int resultXp = _gamificationService.CalculateXPFromVotes(upvotes, downvotes);

        // Assert
        Assert.Equal(expectedXp, resultXp);
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




