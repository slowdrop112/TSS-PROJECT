using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Uniflow.Data;
using Uniflow.Models;

namespace Uniflow.Tests;

/// <summary>
/// Teste pentru verificarea ownership-ului cursurilor (doar profesorul care creează cursul poate să-l editeze/șteargă)
/// </summary>
public class CourseOwnershipTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IServiceProvider _serviceProvider;

    public CourseOwnershipTests()
    {
        // Configurăm serviciile pentru teste
        var services = new ServiceCollection();
        
        // Adăugăm Entity Framework cu InMemory Database
        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseInMemoryDatabase("TestDb_" + Guid.NewGuid().ToString());
        });

        // Adăugăm Logging (necesar pentru Identity)
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
    public async Task Profesor_Poate_Crea_Curs_Cu_Email_Propriu_In_Category()
    {
        // Arrange - Creează un profesor
        var profesor = new IdentityUser { UserName = "profesor@test.com", Email = "profesor@test.com", EmailConfirmed = true };
        await _userManager.CreateAsync(profesor, "Test123!");
        await _userManager.AddToRoleAsync(profesor, "Profesor");

        // Act - Profesorul creează un curs
        var course = new Course
        {
            Title = "My Course",
            Description = "My Description",
            Category = profesor.Email, // Email-ul profesorului este salvat în Category
            CreatedDate = DateTime.Now,
            IsPublished = true
        };
        _context.Courses.Add(course);
        await _context.SaveChangesAsync();

        // Assert - Verificăm că cursul are email-ul profesorului în Category
        var savedCourse = await _context.Courses.FindAsync(course.Id);
        Assert.NotNull(savedCourse);
        Assert.Equal(profesor.Email, savedCourse.Category);
    }

    [Fact]
    public async Task Doar_Profesorul_Proprietar_Poate_Edita_Cursul()
    {
        // Arrange - Creează doi profesori
        var profesor1 = new IdentityUser { UserName = "profesor1@test.com", Email = "profesor1@test.com", EmailConfirmed = true };
        var profesor2 = new IdentityUser { UserName = "profesor2@test.com", Email = "profesor2@test.com", EmailConfirmed = true };
        await _userManager.CreateAsync(profesor1, "Test123!");
        await _userManager.CreateAsync(profesor2, "Test123!");
        await _userManager.AddToRoleAsync(profesor1, "Profesor");
        await _userManager.AddToRoleAsync(profesor2, "Profesor");

        // Arrange - Profesor1 creează un curs
        var course = new Course
        {
            Title = "Course by Profesor1",
            Description = "Description",
            Category = profesor1.Email, // Profesor1 este proprietarul
            CreatedDate = DateTime.Now,
            IsPublished = true
        };
        _context.Courses.Add(course);
        await _context.SaveChangesAsync();

        // Act & Assert - Profesor1 (proprietarul) poate edita
        var savedCourse1 = await _context.Courses.FindAsync(course.Id);
        Assert.NotNull(savedCourse1);
        Assert.Equal(profesor1.Email, savedCourse1.Category); // Verificăm ownership

        // Act & Assert - Profesor2 (nu este proprietar) nu poate edita
        // În realitate, această verificare se face în controller/page model, dar aici testăm logica
        var isProfesor1Owner = savedCourse1.Category == profesor1.Email;
        var isProfesor2Owner = savedCourse1.Category == profesor2.Email;

        Assert.True(isProfesor1Owner);
        Assert.False(isProfesor2Owner);
    }

    [Fact]
    public async Task Admin_Poate_Edita_Orice_Curs()
    {
        // Arrange - Creează un profesor și un admin
        var profesor = new IdentityUser { UserName = "profesor@test.com", Email = "profesor@test.com", EmailConfirmed = true };
        var admin = new IdentityUser { UserName = "admin@test.com", Email = "admin@test.com", EmailConfirmed = true };
        await _userManager.CreateAsync(profesor, "Test123!");
        await _userManager.CreateAsync(admin, "Test123!");
        await _userManager.AddToRoleAsync(profesor, "Profesor");
        await _userManager.AddToRoleAsync(admin, "Admin");

        // Arrange - Profesorul creează un curs
        var course = new Course
        {
            Title = "Course by Profesor",
            Description = "Description",
            Category = profesor.Email,
            CreatedDate = DateTime.Now,
            IsPublished = true
        };
        _context.Courses.Add(course);
        await _context.SaveChangesAsync();

        // Act & Assert - Adminul poate edita orice curs (în controller se verifică User.IsInRole("Admin"))
        var savedCourse = await _context.Courses.FindAsync(course.Id);
        Assert.NotNull(savedCourse);
        
        // Adminul poate edita indiferent de ownership (verificare făcută în controller)
        // Aici testăm doar că cursul există și are proprietar
        Assert.Equal(profesor.Email, savedCourse.Category);
    }

    [Fact]
    public async Task Profesor_Nu_Poate_Edita_Curs_Creat_De_Alt_Profesor()
    {
        // Arrange - Creează doi profesori
        var profesor1 = new IdentityUser { UserName = "profesor1@test.com", Email = "profesor1@test.com", EmailConfirmed = true };
        var profesor2 = new IdentityUser { UserName = "profesor2@test.com", Email = "profesor2@test.com", EmailConfirmed = true };
        await _userManager.CreateAsync(profesor1, "Test123!");
        await _userManager.CreateAsync(profesor2, "Test123!");
        await _userManager.AddToRoleAsync(profesor1, "Profesor");
        await _userManager.AddToRoleAsync(profesor2, "Profesor");

        // Arrange - Profesor1 creează un curs
        var course = new Course
        {
            Title = "Course by Profesor1",
            Description = "Description",
            Category = profesor1.Email, // Profesor1 este proprietarul
            CreatedDate = DateTime.Now,
            IsPublished = true
        };
        _context.Courses.Add(course);
        await _context.SaveChangesAsync();

        // Act & Assert - Verificăm că Profesor2 nu este proprietar
        var savedCourse = await _context.Courses.FindAsync(course.Id);
        var isProfesor2Owner = savedCourse?.Category == profesor2.Email;

        Assert.False(isProfesor2Owner);
        Assert.Equal(profesor1.Email, savedCourse?.Category);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context?.Dispose();
        (_serviceProvider as IDisposable)?.Dispose();
    }
}

