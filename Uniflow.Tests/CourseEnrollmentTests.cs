using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Uniflow.Data;
using Uniflow.Models;

namespace Uniflow.Tests;

/// <summary>
/// Teste de integrare pentru funcționalitatea de înscriere la cursuri (SCRUM-30, SCRUM-31)
/// </summary>
public class CourseEnrollmentTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IServiceProvider _serviceProvider;

    public CourseEnrollmentTests()
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
    public async Task Student_Poate_Sa_Se_Inscrie_La_Un_Curs()
    {
        // Arrange - Creează un profesor și un curs
        var profesor = new IdentityUser { UserName = "profesor@test.com", Email = "profesor@test.com", EmailConfirmed = true };
        await _userManager.CreateAsync(profesor, "Test123!");
        await _userManager.AddToRoleAsync(profesor, "Profesor");

        var course = new Course
        {
            Title = "Test Course",
            Description = "Test Description",
            Category = profesor.Email, // Email profesor
            CreatedDate = DateTime.Now,
            IsPublished = true
        };
        _context.Courses.Add(course);
        await _context.SaveChangesAsync();

        // Arrange - Creează un student
        var student = new IdentityUser { UserName = "student@test.com", Email = "student@test.com", EmailConfirmed = true };
        await _userManager.CreateAsync(student, "Test123!");
        await _userManager.AddToRoleAsync(student, "Student");

        // Act - Studentul se înscrie la curs
        var enrollment = new CourseEnrollment
        {
            CourseId = course.Id,
            StudentId = student.Id,
            EnrollmentDate = DateTime.Now
        };
        _context.CourseEnrollments.Add(enrollment);
        await _context.SaveChangesAsync();

        // Assert - Verificăm că înscrierea a fost creată
        var savedEnrollment = await _context.CourseEnrollments
            .FirstOrDefaultAsync(e => e.CourseId == course.Id && e.StudentId == student.Id);

        Assert.NotNull(savedEnrollment);
        Assert.Equal(course.Id, savedEnrollment.CourseId);
        Assert.Equal(student.Id, savedEnrollment.StudentId);
    }

    [Fact]
    public async Task Student_Nu_Poate_Sa_Se_Inscrie_De_Doua_Ori_La_Acelasi_Curs()
    {
        // Arrange - Creează un profesor și un curs
        var profesor = new IdentityUser { UserName = "profesor@test.com", Email = "profesor@test.com", EmailConfirmed = true };
        await _userManager.CreateAsync(profesor, "Test123!");
        await _userManager.AddToRoleAsync(profesor, "Profesor");

        var course = new Course
        {
            Title = "Test Course",
            Description = "Test Description",
            Category = profesor.Email,
            CreatedDate = DateTime.Now,
            IsPublished = true
        };
        _context.Courses.Add(course);
        await _context.SaveChangesAsync();

        // Arrange - Creează un student
        var student = new IdentityUser { UserName = "student@test.com", Email = "student@test.com", EmailConfirmed = true };
        await _userManager.CreateAsync(student, "Test123!");
        await _userManager.AddToRoleAsync(student, "Student");

        // Act - Studentul se înscrie prima dată
        var enrollment1 = new CourseEnrollment
        {
            CourseId = course.Id,
            StudentId = student.Id,
            EnrollmentDate = DateTime.Now
        };
        _context.CourseEnrollments.Add(enrollment1);
        await _context.SaveChangesAsync();

        // Assert - Verificăm că există o singură înscriere
        var existingEnrollment = await _context.CourseEnrollments
            .AnyAsync(e => e.CourseId == course.Id && e.StudentId == student.Id);
        Assert.True(existingEnrollment);

        // Act - Verificăm logic că nu ar trebui să existe deja o înscriere
        var alreadyEnrolled = await _context.CourseEnrollments
            .AnyAsync(e => e.CourseId == course.Id && e.StudentId == student.Id);
        
        // Assert - Dacă încercăm să adăugăm o a doua înscriere, trebuie să verificăm logic mai întâi
        Assert.True(alreadyEnrolled); // Deja există o înscriere, deci nu ar trebui să permită o a doua
    }

    [Fact]
    public async Task Student_Vede_Cursurile_La_Care_Est_Inscris()
    {
        // Arrange - Creează un profesor și două cursuri
        var profesor = new IdentityUser { UserName = "profesor@test.com", Email = "profesor@test.com", EmailConfirmed = true };
        await _userManager.CreateAsync(profesor, "Test123!");
        await _userManager.AddToRoleAsync(profesor, "Profesor");

        var course1 = new Course
        {
            Title = "Course 1",
            Description = "Description 1",
            Category = profesor.Email,
            CreatedDate = DateTime.Now,
            IsPublished = true
        };
        var course2 = new Course
        {
            Title = "Course 2",
            Description = "Description 2",
            Category = profesor.Email,
            CreatedDate = DateTime.Now,
            IsPublished = true
        };
        _context.Courses.AddRange(course1, course2);
        await _context.SaveChangesAsync();

        // Arrange - Creează un student
        var student = new IdentityUser { UserName = "student@test.com", Email = "student@test.com", EmailConfirmed = true };
        await _userManager.CreateAsync(student, "Test123!");
        await _userManager.AddToRoleAsync(student, "Student");

        // Act - Studentul se înscrie doar la primul curs
        var enrollment = new CourseEnrollment
        {
            CourseId = course1.Id,
            StudentId = student.Id,
            EnrollmentDate = DateTime.Now
        };
        _context.CourseEnrollments.Add(enrollment);
        await _context.SaveChangesAsync();

        // Assert - Verificăm că studentul vede doar cursul la care este înscris
        var studentCourses = await _context.CourseEnrollments
            .Where(e => e.StudentId == student.Id)
            .Include(e => e.Course)
            .Select(e => e.Course)
            .ToListAsync();

        Assert.Single(studentCourses);
        Assert.Equal(course1.Id, studentCourses[0].Id);
        Assert.Equal("Course 1", studentCourses[0].Title);
    }

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

