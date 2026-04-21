using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Uniflow.Data;
using Uniflow.Models;
using Uniflow.Services;
using Xunit;

namespace Uniflow.Tests
{
    public class DashboardTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly DashboardService _dashboardService;
        private readonly IServiceProvider _serviceProvider;

        public DashboardTests()
        {
            // Configurare servicii pentru teste
            var services = new ServiceCollection();

            // Baza de date InMemory
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase(databaseName: $"DashboardTestDb_{Guid.NewGuid()}"));

            // Identity
            services.AddIdentityCore<IdentityUser>(options =>
            {
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredLength = 1;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>();

            // Logger
            services.AddLogging();

            // DashboardService
            services.AddScoped<DashboardService>();

            _serviceProvider = services.BuildServiceProvider();
            _context = _serviceProvider.GetRequiredService<ApplicationDbContext>();
            _userManager = _serviceProvider.GetRequiredService<UserManager<IdentityUser>>();
            _roleManager = _serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            _dashboardService = _serviceProvider.GetRequiredService<DashboardService>();

            // Creează rolurile
            SeedRolesAsync().Wait();
        }

        private async Task SeedRolesAsync()
        {
            var roles = new[] { "Admin", "Profesor", "Student" };
            foreach (var roleName in roles)
            {
                if (!await _roleManager.RoleExistsAsync(roleName))
                {
                    await _roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }
        }

        [Fact]
        public async Task GetDashboardStatisticsAsync_Should_Return_Zero_When_Database_Is_Empty()
        {
            // Act
            var stats = await _dashboardService.GetDashboardStatisticsAsync();

            // Assert
            Assert.NotNull(stats);
            Assert.Equal(0, stats.TotalStudents);
            Assert.Equal(0, stats.TotalProfessors);
            Assert.Equal(0, stats.TotalCourses);
            Assert.Equal(0, stats.TotalEnrollments);
            Assert.Equal(0, stats.TotalNotes);
            Assert.Equal(0, stats.TotalVotes);
        }

        [Fact]
        public async Task GetDashboardStatisticsAsync_Should_Count_Users_By_Role_Correctly()
        {
            // Arrange
            var student1 = new IdentityUser { Email = "student1@test.com", UserName = "student1@test.com" };
            var student2 = new IdentityUser { Email = "student2@test.com", UserName = "student2@test.com" };
            var professor1 = new IdentityUser { Email = "prof1@test.com", UserName = "prof1@test.com" };
            var admin1 = new IdentityUser { Email = "admin1@test.com", UserName = "admin1@test.com" };

            await _userManager.CreateAsync(student1);
            await _userManager.CreateAsync(student2);
            await _userManager.CreateAsync(professor1);
            await _userManager.CreateAsync(admin1);

            await _userManager.AddToRoleAsync(student1, "Student");
            await _userManager.AddToRoleAsync(student2, "Student");
            await _userManager.AddToRoleAsync(professor1, "Profesor");
            await _userManager.AddToRoleAsync(admin1, "Admin");

            // Act
            var stats = await _dashboardService.GetDashboardStatisticsAsync();

            // Assert
            Assert.Equal(2, stats.TotalStudents);
            Assert.Equal(1, stats.TotalProfessors);
            Assert.Equal(1, stats.TotalAdmins);
            Assert.Equal(4, stats.TotalUsers);
        }

        [Fact]
        public async Task GetDashboardStatisticsAsync_Should_Count_Courses_Correctly()
        {
            // Arrange
            var course1 = new Course
            {
                Title = "Curs 1",
                Description = "Descriere 1",
                Category = "Test",
                CreatedDate = DateTime.UtcNow,
                IsPublished = true
            };
            var course2 = new Course
            {
                Title = "Curs 2",
                Description = "Descriere 2",
                Category = "Test",
                CreatedDate = DateTime.UtcNow,
                IsPublished = false
            };

            _context.Courses.Add(course1);
            _context.Courses.Add(course2);
            await _context.SaveChangesAsync();

            // Act
            var stats = await _dashboardService.GetDashboardStatisticsAsync();

            // Assert
            Assert.Equal(2, stats.TotalCourses);
            Assert.Equal(1, stats.PublishedCourses);
            Assert.Equal(1, stats.UnpublishedCourses);
        }

        [Fact]
        public async Task GetDashboardStatisticsAsync_Should_Count_Enrollments_Correctly()
        {
            // Arrange
            var student = new IdentityUser { Email = "student@test.com", UserName = "student@test.com" };
            await _userManager.CreateAsync(student);
            await _userManager.AddToRoleAsync(student, "Student");

            var course = new Course
            {
                Title = "Curs Test",
                Description = "Descriere",
                Category = "Test",
                CreatedDate = DateTime.UtcNow,
                IsPublished = true
            };
            _context.Courses.Add(course);
            await _context.SaveChangesAsync();

            var enrollment = new CourseEnrollment
            {
                CourseId = course.Id,
                StudentId = student.Id,
                EnrollmentDate = DateTime.UtcNow
            };
            _context.CourseEnrollments.Add(enrollment);
            await _context.SaveChangesAsync();

            // Act
            var stats = await _dashboardService.GetDashboardStatisticsAsync();

            // Assert
            Assert.Equal(1, stats.TotalEnrollments);
        }

        [Fact]
        public async Task GetDashboardStatisticsAsync_Should_Count_Notes_By_Status_Correctly()
        {
            // Arrange
            var student = new IdentityUser { Email = "student@test.com", UserName = "student@test.com" };
            await _userManager.CreateAsync(student);

            var course = new Course
            {
                Title = "Curs Test",
                Description = "Descriere",
                Category = "Test",
                CreatedDate = DateTime.UtcNow,
                IsPublished = true
            };
            _context.Courses.Add(course);
            await _context.SaveChangesAsync();

            var note1 = new Note
            {
                Title = "Notiță 1",
                Content = "Conținut 1",
                CourseId = course.Id,
                StudentId = student.Id,
                Status = "Pending",
                CreatedDate = DateTime.UtcNow
            };
            var note2 = new Note
            {
                Title = "Notiță 2",
                Content = "Conținut 2",
                CourseId = course.Id,
                StudentId = student.Id,
                Status = "Approved",
                CreatedDate = DateTime.UtcNow
            };
            var note3 = new Note
            {
                Title = "Notiță 3",
                Content = "Conținut 3",
                CourseId = course.Id,
                StudentId = student.Id,
                Status = "Rejected",
                CreatedDate = DateTime.UtcNow
            };

            _context.Notes.Add(note1);
            _context.Notes.Add(note2);
            _context.Notes.Add(note3);
            await _context.SaveChangesAsync();

            // Act
            var stats = await _dashboardService.GetDashboardStatisticsAsync();

            // Assert
            Assert.Equal(3, stats.TotalNotes);
            Assert.Equal(1, stats.PendingNotes);
            Assert.Equal(1, stats.ApprovedNotes);
            Assert.Equal(1, stats.RejectedNotes);
        }

        [Fact]
        public async Task GetDashboardStatisticsAsync_Should_Count_Votes_Correctly()
        {
            // Arrange
            var student = new IdentityUser { Email = "student@test.com", UserName = "student@test.com" };
            await _userManager.CreateAsync(student);

            var course = new Course
            {
                Title = "Curs Test",
                Description = "Descriere",
                Category = "Test",
                CreatedDate = DateTime.UtcNow,
                IsPublished = true
            };
            _context.Courses.Add(course);
            await _context.SaveChangesAsync();

            var note = new Note
            {
                Title = "Notiță",
                Content = "Conținut",
                CourseId = course.Id,
                StudentId = student.Id,
                Status = "Pending",
                CreatedDate = DateTime.UtcNow
            };
            _context.Notes.Add(note);
            await _context.SaveChangesAsync();

            var upvote = new NoteVote
            {
                NoteId = note.Id,
                UserId = student.Id,
                IsUpvote = true,
                VoteDate = DateTime.UtcNow
            };
            var downvote = new NoteVote
            {
                NoteId = note.Id,
                UserId = student.Id,
                IsUpvote = false,
                VoteDate = DateTime.UtcNow
            };

            _context.NoteVotes.Add(upvote);
            _context.NoteVotes.Add(downvote);
            await _context.SaveChangesAsync();

            // Act
            var stats = await _dashboardService.GetDashboardStatisticsAsync();

            // Assert
            Assert.Equal(2, stats.TotalVotes);
            Assert.Equal(1, stats.TotalUpvotes);
            Assert.Equal(1, stats.TotalDownvotes);
        }

        [Fact]
        public async Task GetDashboardStatisticsAsync_Should_Calculate_XP_Statistics_Correctly()
        {
            // Arrange
            var student1 = new IdentityUser { Email = "student1@test.com", UserName = "student1@test.com" };
            var student2 = new IdentityUser { Email = "student2@test.com", UserName = "student2@test.com" };
            await _userManager.CreateAsync(student1);
            await _userManager.CreateAsync(student2);

            var profile1 = new UserProfile
            {
                UserId = student1.Id,
                FirstName = "Student",
                LastName = "1",
                XP = 100
            };
            var profile2 = new UserProfile
            {
                UserId = student2.Id,
                FirstName = "Student",
                LastName = "2",
                XP = 200
            };

            _context.UserProfiles.Add(profile1);
            _context.UserProfiles.Add(profile2);
            await _context.SaveChangesAsync();

            // Act
            var stats = await _dashboardService.GetDashboardStatisticsAsync();

            // Assert
            Assert.Equal(300, stats.TotalXP);
            Assert.Equal(150, stats.AverageXP); // (100 + 200) / 2 = 150
            Assert.NotNull(stats.TopXPUser);
            Assert.Equal(student2.Id, stats.TopXPUser.UserId);
            Assert.Equal(200, stats.TopXPUser.XP);
        }

        [Fact]
        public async Task GetDashboardTrendsAsync_Should_Count_Recent_Activity_Correctly()
        {
            // Arrange
            var course = new Course
            {
                Title = "Curs Recent",
                Description = "Descriere",
                Category = "Test",
                CreatedDate = DateTime.UtcNow.AddDays(-10), // În ultimele 30 zile
                IsPublished = true
            };
            _context.Courses.Add(course);
            await _context.SaveChangesAsync();

            var student = new IdentityUser { Email = "student@test.com", UserName = "student@test.com" };
            await _userManager.CreateAsync(student);

            var enrollment = new CourseEnrollment
            {
                CourseId = course.Id,
                StudentId = student.Id,
                EnrollmentDate = DateTime.UtcNow.AddDays(-5)
            };
            _context.CourseEnrollments.Add(enrollment);

            var note = new Note
            {
                Title = "Notiță Recentă",
                Content = "Conținut",
                CourseId = course.Id,
                StudentId = student.Id,
                Status = "Pending",
                CreatedDate = DateTime.UtcNow.AddDays(-3)
            };
            _context.Notes.Add(note);
            await _context.SaveChangesAsync();

            // Act
            var trends = await _dashboardService.GetDashboardTrendsAsync(30);

            // Assert
            Assert.Equal(1, trends.NewCoursesLast30Days);
            Assert.Equal(1, trends.NewEnrollmentsLast30Days);
            Assert.Equal(1, trends.NewNotesLast30Days);
        }

        [Fact]
        public async Task GetTopCoursesAsync_Should_Return_Courses_Ordered_By_Enrollments()
        {
            // Arrange
            var course1 = new Course
            {
                Title = "Curs Popular",
                Description = "Descriere",
                Category = "Test",
                CreatedDate = DateTime.UtcNow,
                IsPublished = true
            };
            var course2 = new Course
            {
                Title = "Curs Mai Puțin Popular",
                Description = "Descriere",
                Category = "Test",
                CreatedDate = DateTime.UtcNow,
                IsPublished = true
            };
            _context.Courses.Add(course1);
            _context.Courses.Add(course2);
            await _context.SaveChangesAsync();

            var student1 = new IdentityUser { Email = "student1@test.com", UserName = "student1@test.com" };
            var student2 = new IdentityUser { Email = "student2@test.com", UserName = "student2@test.com" };
            var student3 = new IdentityUser { Email = "student3@test.com", UserName = "student3@test.com" };
            await _userManager.CreateAsync(student1);
            await _userManager.CreateAsync(student2);
            await _userManager.CreateAsync(student3);

            // Curs 1: 3 înscrieri
            _context.CourseEnrollments.Add(new CourseEnrollment { CourseId = course1.Id, StudentId = student1.Id, EnrollmentDate = DateTime.UtcNow });
            _context.CourseEnrollments.Add(new CourseEnrollment { CourseId = course1.Id, StudentId = student2.Id, EnrollmentDate = DateTime.UtcNow });
            _context.CourseEnrollments.Add(new CourseEnrollment { CourseId = course1.Id, StudentId = student3.Id, EnrollmentDate = DateTime.UtcNow });

            // Curs 2: 1 înscriere
            _context.CourseEnrollments.Add(new CourseEnrollment { CourseId = course2.Id, StudentId = student1.Id, EnrollmentDate = DateTime.UtcNow });

            await _context.SaveChangesAsync();

            // Act
            var topCourses = await _dashboardService.GetTopCoursesAsync(5);

            // Assert
            Assert.Equal(2, topCourses.Count);
            Assert.Equal("Curs Popular", topCourses[0].Title);
            Assert.Equal(3, topCourses[0].EnrollmentCount);
            Assert.Equal("Curs Mai Puțin Popular", topCourses[1].Title);
            Assert.Equal(1, topCourses[1].EnrollmentCount);
        }

        [Fact]
        public async Task GetTopStudentsAsync_Should_Return_Students_Ordered_By_XP()
        {
            // Arrange
            var student1 = new IdentityUser { Email = "student1@test.com", UserName = "student1@test.com" };
            var student2 = new IdentityUser { Email = "student2@test.com", UserName = "student2@test.com" };
            var student3 = new IdentityUser { Email = "student3@test.com", UserName = "student3@test.com" };
            await _userManager.CreateAsync(student1);
            await _userManager.CreateAsync(student2);
            await _userManager.CreateAsync(student3);

            var profile1 = new UserProfile { UserId = student1.Id, FirstName = "S1", LastName = "L1", XP = 50 };
            var profile2 = new UserProfile { UserId = student2.Id, FirstName = "S2", LastName = "L2", XP = 200 };
            var profile3 = new UserProfile { UserId = student3.Id, FirstName = "S3", LastName = "L3", XP = 100 };

            _context.UserProfiles.Add(profile1);
            _context.UserProfiles.Add(profile2);
            _context.UserProfiles.Add(profile3);
            await _context.SaveChangesAsync();

            // Act
            var topStudents = await _dashboardService.GetTopStudentsAsync(5);

            // Assert
            Assert.Equal(3, topStudents.Count);
            Assert.Equal("student2@test.com", topStudents[0].Email);
            Assert.Equal(200, topStudents[0].XP);
            Assert.Equal("student3@test.com", topStudents[1].Email);
            Assert.Equal(100, topStudents[1].XP);
            Assert.Equal("student1@test.com", topStudents[2].Email);
            Assert.Equal(50, topStudents[2].XP);
        }

        public void Dispose()
        {
            _context?.Database.EnsureDeleted();
            _context?.Dispose();
        }
    }
}

