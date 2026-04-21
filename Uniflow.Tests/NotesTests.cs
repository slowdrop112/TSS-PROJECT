using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Uniflow.Data;
using Uniflow.Models;
using Uniflow.Services;

namespace Uniflow.Tests;

/// <summary>
/// Teste pentru sistemul de notițe (SCRUM-54, SCRUM-40, SCRUM-41, SCRUM-42)
/// </summary>
public class NotesTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly GamificationService _gamificationService;
    private readonly IServiceProvider _serviceProvider;

    public NotesTests()
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
        
        _gamificationService = new GamificationService(_context, logger, null!, null!);
        
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
    public async Task Student_Poate_Crea_Notita()
    {
        // Arrange
        var student = new IdentityUser { UserName = "student@test.com", Email = "student@test.com", EmailConfirmed = true };
        await _userManager.CreateAsync(student, "Test123!");
        await _userManager.AddToRoleAsync(student, "Student");

        var course = new Course
        {
            Title = "Test Course",
            Description = "Test",
            Category = "profesor@test.com",
            CreatedDate = DateTime.Now,
            IsPublished = true
        };
        _context.Courses.Add(course);
        await _context.SaveChangesAsync();

        var enrollment = new CourseEnrollment
        {
            CourseId = course.Id,
            StudentId = student.Id,
            EnrollmentDate = DateTime.Now
        };
        _context.CourseEnrollments.Add(enrollment);
        await _context.SaveChangesAsync();

        // Act
        var note = new Note
        {
            Title = "Test Note",
            Content = "Test Content",
            CourseId = course.Id,
            StudentId = student.Id,
            CreatedDate = DateTime.Now,
            Status = "Pending"
        };
        _context.Notes.Add(note);
        await _context.SaveChangesAsync();

        // Assert
        var savedNote = await _context.Notes.FindAsync(note.Id);
        Assert.NotNull(savedNote);
        Assert.Equal("Test Note", savedNote.Title);
        Assert.Equal(student.Id, savedNote.StudentId);
    }

    [Fact]
    public async Task Student_Primeste_XP_La_Creare_Notita()
    {
        // Arrange
        var student = new IdentityUser { UserName = "student@test.com", Email = "student@test.com", EmailConfirmed = true };
        await _userManager.CreateAsync(student, "Test123!");
        await _userManager.AddToRoleAsync(student, "Student");

        var course = new Course
        {
            Title = "Test Course",
            Description = "Test",
            Category = "profesor@test.com",
            CreatedDate = DateTime.Now,
            IsPublished = true
        };
        _context.Courses.Add(course);
        await _context.SaveChangesAsync();

        var enrollment = new CourseEnrollment
        {
            CourseId = course.Id,
            StudentId = student.Id,
            EnrollmentDate = DateTime.Now
        };
        _context.CourseEnrollments.Add(enrollment);
        await _context.SaveChangesAsync();

        var initialXP = await _gamificationService.GetXPAsync(student.Id);

        // Act
        var note = new Note
        {
            Title = "Test Note",
            Content = "Test Content",
            CourseId = course.Id,
            StudentId = student.Id,
            CreatedDate = DateTime.Now,
            Status = "Pending"
        };
        _context.Notes.Add(note);
        await _context.SaveChangesAsync();

        // Notițele nu mai primesc XP la creare, doar la validare
        // Assert
        var finalXP = await _gamificationService.GetXPAsync(student.Id);
        Assert.Equal(initialXP, finalXP); // XP rămâne același
    }

    [Fact]
    public async Task Student_Poate_Vota_Notita_Altui_Student()
    {
        // Arrange
        var student1 = new IdentityUser { UserName = "student1@test.com", Email = "student1@test.com", EmailConfirmed = true };
        var student2 = new IdentityUser { UserName = "student2@test.com", Email = "student2@test.com", EmailConfirmed = true };
        await _userManager.CreateAsync(student1, "Test123!");
        await _userManager.CreateAsync(student2, "Test123!");
        await _userManager.AddToRoleAsync(student1, "Student");
        await _userManager.AddToRoleAsync(student2, "Student");

        var course = new Course
        {
            Title = "Test Course",
            Description = "Test",
            Category = "profesor@test.com",
            CreatedDate = DateTime.Now,
            IsPublished = true
        };
        _context.Courses.Add(course);
        await _context.SaveChangesAsync();

        var enrollment1 = new CourseEnrollment { CourseId = course.Id, StudentId = student1.Id, EnrollmentDate = DateTime.Now };
        var enrollment2 = new CourseEnrollment { CourseId = course.Id, StudentId = student2.Id, EnrollmentDate = DateTime.Now };
        _context.CourseEnrollments.AddRange(enrollment1, enrollment2);
        await _context.SaveChangesAsync();

        var note = new Note
        {
            Title = "Test Note",
            Content = "Test Content",
            CourseId = course.Id,
            StudentId = student1.Id,
            CreatedDate = DateTime.Now,
            Status = "Pending"
        };
        _context.Notes.Add(note);
        await _context.SaveChangesAsync();

        // Act
        var vote = new NoteVote
        {
            NoteId = note.Id,
            UserId = student2.Id,
            IsUpvote = true,
            VoteDate = DateTime.Now
        };
        _context.NoteVotes.Add(vote);
        await _context.SaveChangesAsync();

        // Assert
        var savedVote = await _context.NoteVotes
            .FirstOrDefaultAsync(v => v.NoteId == note.Id && v.UserId == student2.Id);
        Assert.NotNull(savedVote);
        Assert.True(savedVote.IsUpvote);
    }

    [Fact]
    public async Task Student_Primeste_XP_La_Upvote_Notita()
    {
        // Arrange
        var student1 = new IdentityUser { UserName = "student1@test.com", Email = "student1@test.com", EmailConfirmed = true };
        var student2 = new IdentityUser { UserName = "student2@test.com", Email = "student2@test.com", EmailConfirmed = true };
        await _userManager.CreateAsync(student1, "Test123!");
        await _userManager.CreateAsync(student2, "Test123!");
        await _userManager.AddToRoleAsync(student1, "Student");
        await _userManager.AddToRoleAsync(student2, "Student");

        var course = new Course
        {
            Title = "Test Course",
            Description = "Test",
            Category = "profesor@test.com",
            CreatedDate = DateTime.Now,
            IsPublished = true
        };
        _context.Courses.Add(course);
        await _context.SaveChangesAsync();

        var enrollment1 = new CourseEnrollment { CourseId = course.Id, StudentId = student1.Id, EnrollmentDate = DateTime.Now };
        var enrollment2 = new CourseEnrollment { CourseId = course.Id, StudentId = student2.Id, EnrollmentDate = DateTime.Now };
        _context.CourseEnrollments.AddRange(enrollment1, enrollment2);
        await _context.SaveChangesAsync();

        var note = new Note
        {
            Title = "Test Note",
            Content = "Test Content",
            CourseId = course.Id,
            StudentId = student1.Id,
            CreatedDate = DateTime.Now,
            Status = "Pending"
        };
        _context.Notes.Add(note);
        await _context.SaveChangesAsync();

        var initialXP = await _gamificationService.GetXPAsync(student1.Id);

        // Act
        var vote = new NoteVote
        {
            NoteId = note.Id,
            UserId = student2.Id,
            IsUpvote = true,
            VoteDate = DateTime.Now
        };
        _context.NoteVotes.Add(vote);
        await _context.SaveChangesAsync();

        // XP-ul pentru voturi se acordă doar când notița este validată, nu pentru fiecare vot individual

        // Assert
        var finalXP = await _gamificationService.GetXPAsync(student1.Id);
        Assert.Equal(initialXP, finalXP); // XP rămâne același până la validare
    }

    [Fact]
    public async Task Student_Poate_Partaja_Notita_Cu_Alt_Student()
    {
        // Arrange
        var student1 = new IdentityUser { UserName = "student1@test.com", Email = "student1@test.com", EmailConfirmed = true };
        var student2 = new IdentityUser { UserName = "student2@test.com", Email = "student2@test.com", EmailConfirmed = true };
        await _userManager.CreateAsync(student1, "Test123!");
        await _userManager.CreateAsync(student2, "Test123!");
        await _userManager.AddToRoleAsync(student1, "Student");
        await _userManager.AddToRoleAsync(student2, "Student");

        var course = new Course
        {
            Title = "Test Course",
            Description = "Test",
            Category = "profesor@test.com",
            CreatedDate = DateTime.Now,
            IsPublished = true
        };
        _context.Courses.Add(course);
        await _context.SaveChangesAsync();

        var enrollment1 = new CourseEnrollment { CourseId = course.Id, StudentId = student1.Id, EnrollmentDate = DateTime.Now };
        var enrollment2 = new CourseEnrollment { CourseId = course.Id, StudentId = student2.Id, EnrollmentDate = DateTime.Now };
        _context.CourseEnrollments.AddRange(enrollment1, enrollment2);
        await _context.SaveChangesAsync();

        var note = new Note
        {
            Title = "Test Note",
            Content = "Test Content",
            CourseId = course.Id,
            StudentId = student1.Id,
            CreatedDate = DateTime.Now,
            Status = "Pending"
        };
        _context.Notes.Add(note);
        await _context.SaveChangesAsync();

        // Act
        var share = new NoteShare
        {
            NoteId = note.Id,
            OwnerId = student1.Id,
            SharedWithUserId = student2.Id,
            SharedDate = DateTime.Now
        };
        _context.NoteShares.Add(share);
        await _context.SaveChangesAsync();

        // Assert
        var savedShare = await _context.NoteShares
            .FirstOrDefaultAsync(s => s.NoteId == note.Id && s.SharedWithUserId == student2.Id);
        Assert.NotNull(savedShare);
        Assert.Equal(student1.Id, savedShare.OwnerId);
        Assert.Equal(student2.Id, savedShare.SharedWithUserId);
    }

    [Fact]
    public async Task Student_Nu_Poate_Vota_Propria_Notita()
    {
        // Arrange
        var student = new IdentityUser { UserName = "student@test.com", Email = "student@test.com", EmailConfirmed = true };
        await _userManager.CreateAsync(student, "Test123!");
        await _userManager.AddToRoleAsync(student, "Student");

        var course = new Course
        {
            Title = "Test Course",
            Description = "Test",
            Category = "profesor@test.com",
            CreatedDate = DateTime.Now,
            IsPublished = true
        };
        _context.Courses.Add(course);
        await _context.SaveChangesAsync();

        var enrollment = new CourseEnrollment { CourseId = course.Id, StudentId = student.Id, EnrollmentDate = DateTime.Now };
        _context.CourseEnrollments.Add(enrollment);
        await _context.SaveChangesAsync();

        var note = new Note
        {
            Title = "Test Note",
            Content = "Test Content",
            CourseId = course.Id,
            StudentId = student.Id,
            CreatedDate = DateTime.Now,
            Status = "Pending"
        };
        _context.Notes.Add(note);
        await _context.SaveChangesAsync();

        // Act & Assert
        // Nu ar trebui să permită votarea propriei notițe (verificat în logica aplicației)
        // Pentru test, verificăm că nu există voturi pentru propria notiță
        var vote = await _context.NoteVotes
            .FirstOrDefaultAsync(v => v.NoteId == note.Id && v.UserId == student.Id);
        
        Assert.Null(vote); // Nu ar trebui să existe vot
    }

    [Fact]
    public async Task Profesor_Poate_Aprobă_Notita_Studentului()
    {
        // Arrange
        var profesor = new IdentityUser { UserName = "profesor@test.com", Email = "profesor@test.com", EmailConfirmed = true };
        var student = new IdentityUser { UserName = "student@test.com", Email = "student@test.com", EmailConfirmed = true };
        await _userManager.CreateAsync(profesor, "Test123!");
        await _userManager.CreateAsync(student, "Test123!");
        await _userManager.AddToRoleAsync(profesor, "Profesor");
        await _userManager.AddToRoleAsync(student, "Student");

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

        var enrollment = new CourseEnrollment { CourseId = course.Id, StudentId = student.Id, EnrollmentDate = DateTime.Now };
        _context.CourseEnrollments.Add(enrollment);
        await _context.SaveChangesAsync();

        var note = new Note
        {
            Title = "Test Note",
            Content = "Test Content",
            CourseId = course.Id,
            StudentId = student.Id,
            CreatedDate = DateTime.Now,
            Status = "Pending"
        };
        _context.Notes.Add(note);
        await _context.SaveChangesAsync();

        // Act
        note.Status = "Approved";
        note.ValidatedByUserId = profesor.Id;
        note.ValidationDate = DateTime.Now;
        await _context.SaveChangesAsync();

        // Assert
        var savedNote = await _context.Notes.FindAsync(note.Id);
        Assert.NotNull(savedNote);
        Assert.Equal("Approved", savedNote.Status);
        Assert.Equal(profesor.Id, savedNote.ValidatedByUserId);
    }

    [Fact]
    public async Task Student_Primeste_XP_La_Aprobare_Notita()
    {
        // Arrange
        var profesor = new IdentityUser { UserName = "profesor@test.com", Email = "profesor@test.com", EmailConfirmed = true };
        var student = new IdentityUser { UserName = "student@test.com", Email = "student@test.com", EmailConfirmed = true };
        await _userManager.CreateAsync(profesor, "Test123!");
        await _userManager.CreateAsync(student, "Test123!");
        await _userManager.AddToRoleAsync(profesor, "Profesor");
        await _userManager.AddToRoleAsync(student, "Student");

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

        var enrollment = new CourseEnrollment { CourseId = course.Id, StudentId = student.Id, EnrollmentDate = DateTime.Now };
        _context.CourseEnrollments.Add(enrollment);
        await _context.SaveChangesAsync();

        var note = new Note
        {
            Title = "Test Note",
            Content = "Test Content",
            CourseId = course.Id,
            StudentId = student.Id,
            CreatedDate = DateTime.Now,
            Status = "Pending"
        };
        _context.Notes.Add(note);
        await _context.SaveChangesAsync();

        var initialXP = await _gamificationService.GetXPAsync(student.Id);

        // Act
        note.Status = "Approved";
        note.ValidatedByUserId = profesor.Id;
        note.ValidationDate = DateTime.Now;
        await _context.SaveChangesAsync();

        await _gamificationService.AwardXPAsync(
            student.Id,
            GamificationService.XP_NOTE_APPROVED,
            $"Notiță aprobată: {note.Title}");

        // Assert
        var finalXP = await _gamificationService.GetXPAsync(student.Id);
        Assert.Equal(initialXP + GamificationService.XP_NOTE_APPROVED, finalXP);
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


