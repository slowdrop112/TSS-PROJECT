using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Uniflow.Data;
using Uniflow.Models;
using Uniflow.Services;
using Xunit;

namespace Uniflow.Tests
{
    public class NotificationTests
    {
        private ApplicationDbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new ApplicationDbContext(options);
        }

        private NotificationService GetNotificationService(ApplicationDbContext context)
        {
            var logger = new LoggerFactory().CreateLogger<NotificationService>();
            return new NotificationService(context, logger);
        }

        [Fact]
        public async Task NotifyNoteApprovedAsync_CreatesNotification()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var service = GetNotificationService(context);

            var studentId = "student1";
            var noteId = 1;
            var noteTitle = "Test Note";
            var courseTitle = "Test Course";

            // Act
            await service.NotifyNoteApprovedAsync(noteId, studentId, noteTitle, courseTitle);

            // Assert
            var notification = await context.UserNotifications.FirstOrDefaultAsync();
            Assert.NotNull(notification);
            Assert.Equal(studentId, notification.UserId);
            Assert.Contains("Aprobată", notification.Title);
            Assert.Contains(noteTitle, notification.Message);
            Assert.Contains(courseTitle, notification.Message);
            Assert.Equal("success", notification.Type);
            Assert.False(notification.IsRead);
            Assert.Contains($"/Notes/Details/{noteId}", notification.LinkUrl);
        }

        [Fact]
        public async Task NotifyNoteRejectedAsync_CreatesNotification()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var service = GetNotificationService(context);

            var studentId = "student1";
            var noteId = 1;
            var noteTitle = "Test Note";
            var courseTitle = "Test Course";

            // Act
            await service.NotifyNoteRejectedAsync(noteId, studentId, noteTitle, courseTitle);

            // Assert
            var notification = await context.UserNotifications.FirstOrDefaultAsync();
            Assert.NotNull(notification);
            Assert.Equal(studentId, notification.UserId);
            Assert.Contains("Respinsă", notification.Title);
            Assert.Contains(noteTitle, notification.Message);
            Assert.Contains(courseTitle, notification.Message);
            Assert.Equal("warning", notification.Type);
            Assert.False(notification.IsRead);
        }

        [Fact]
        public async Task NotifyNoteReceivedVoteAsync_CreatesUpvoteNotification()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var service = GetNotificationService(context);

            var authorId = "author1";
            var noteId = 1;
            var noteTitle = "Test Note";

            // Act
            await service.NotifyNoteReceivedVoteAsync(noteId, authorId, noteTitle, true);

            // Assert
            var notification = await context.UserNotifications.FirstOrDefaultAsync();
            Assert.NotNull(notification);
            Assert.Equal(authorId, notification.UserId);
            Assert.Contains("upvote", notification.Title);
            Assert.Contains(noteTitle, notification.Message);
            Assert.Equal("success", notification.Type);
        }

        [Fact]
        public async Task NotifyNoteReceivedVoteAsync_CreatesDownvoteNotification()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var service = GetNotificationService(context);

            var authorId = "author1";
            var noteId = 1;
            var noteTitle = "Test Note";

            // Act
            await service.NotifyNoteReceivedVoteAsync(noteId, authorId, noteTitle, false);

            // Assert
            var notification = await context.UserNotifications.FirstOrDefaultAsync();
            Assert.NotNull(notification);
            Assert.Equal(authorId, notification.UserId);
            Assert.Contains("downvote", notification.Title);
            Assert.Contains(noteTitle, notification.Message);
            Assert.Equal("info", notification.Type);
        }

        [Fact]
        public async Task NotifyNoteReceivedCommentAsync_CreatesNotification()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var service = GetNotificationService(context);

            var authorId = "author1";
            var noteId = 1;
            var noteTitle = "Test Note";
            var commenterName = "John Doe";

            // Act
            await service.NotifyNoteReceivedCommentAsync(noteId, authorId, noteTitle, commenterName);

            // Assert
            var notification = await context.UserNotifications.FirstOrDefaultAsync();
            Assert.NotNull(notification);
            Assert.Equal(authorId, notification.UserId);
            Assert.Contains("Comentariu", notification.Title);
            Assert.Contains(commenterName, notification.Message);
            Assert.Contains(noteTitle, notification.Message);
            Assert.Equal("info", notification.Type);
        }

        [Fact]
        public async Task NotifyStudentsOfAnnouncementAsync_CreatesNotificationsForAllEnrolledStudents()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var service = GetNotificationService(context);

            var courseId = 1;
            var course = new Course { Id = courseId, Title = "Test Course" };
            context.Courses.Add(course);

            // Add 3 enrolled students
            var student1Id = "student1";
            var student2Id = "student2";
            var student3Id = "student3";

            context.CourseEnrollments.Add(new CourseEnrollment { CourseId = courseId, StudentId = student1Id });
            context.CourseEnrollments.Add(new CourseEnrollment { CourseId = courseId, StudentId = student2Id });
            context.CourseEnrollments.Add(new CourseEnrollment { CourseId = courseId, StudentId = student3Id });

            await context.SaveChangesAsync();

            var announceTitle = "Test Announcement";

            // Act
            await service.NotifyStudentsOfAnnouncementAsync(courseId, announceTitle, "Test Teacher");

            // Assert
            var notifications = await context.UserNotifications.ToListAsync();
            Assert.Equal(3, notifications.Count);
            Assert.Contains(notifications, n => n.UserId == student1Id);
            Assert.Contains(notifications, n => n.UserId == student2Id);
            Assert.Contains(notifications, n => n.UserId == student3Id);
            Assert.All(notifications, n =>
            {
                Assert.Contains("Anunț", n.Title);
                Assert.Contains(announceTitle, n.Message);
                Assert.Contains(course.Title, n.Message);
            });
        }

        [Fact]
        public async Task NotifyProfessorOfNewNoteAsync_CreatesNotification()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var service = GetNotificationService(context);

            var profesorId = "profesor1";
            var courseId = 1;
            var course = new Course { Id = courseId, Title = "Test Course", ProfesorId = profesorId };
            context.Courses.Add(course);
            await context.SaveChangesAsync();

            var noteId = 1;
            var noteTitle = "Test Note";
            var studentName = "Jane Student";

            // Act
            await service.NotifyProfessorOfNewNoteAsync(courseId, noteId, noteTitle, studentName);

            // Assert
            var notification = await context.UserNotifications.FirstOrDefaultAsync();
            Assert.NotNull(notification);
            Assert.Equal(profesorId, notification.UserId);
            Assert.Contains("Notiță Nouă", notification.Title);
            Assert.Contains(studentName, notification.Message);
            Assert.Contains(noteTitle, notification.Message);
            Assert.Contains(course.Title, notification.Message);
            Assert.Contains("/Notes/Validate", notification.LinkUrl);
        }

        [Fact]
        public async Task NotifyProfessorOfEnrollmentAsync_CreatesNotification()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var service = GetNotificationService(context);

            var profesorId = "profesor1";
            var courseId = 1;
            var course = new Course { Id = courseId, Title = "Test Course", ProfesorId = profesorId };
            context.Courses.Add(course);
            await context.SaveChangesAsync();

            var studentName = "New Student";

            // Act
            await service.NotifyProfessorOfEnrollmentAsync(courseId, studentName);

            // Assert
            var notification = await context.UserNotifications.FirstOrDefaultAsync();
            Assert.NotNull(notification);
            Assert.Equal(profesorId, notification.UserId);
            Assert.Contains("Student Nou", notification.Title);
            Assert.Contains(studentName, notification.Message);
            Assert.Contains(course.Title, notification.Message);
            Assert.Equal("success", notification.Type);
        }

        [Fact]
        public async Task NotifyProfessorOfNewNoteAsync_DoesNotCreateNotificationIfCourseHasNoProfesor()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var service = GetNotificationService(context);

            var courseId = 1;
            var course = new Course { Id = courseId, Title = "Test Course", ProfesorId = null };
            context.Courses.Add(course);
            await context.SaveChangesAsync();

            var noteId = 1;
            var noteTitle = "Test Note";
            var studentName = "Jane Student";

            // Act
            await service.NotifyProfessorOfNewNoteAsync(courseId, noteId, noteTitle, studentName);

            // Assert
            var notifications = await context.UserNotifications.ToListAsync();
            Assert.Empty(notifications);
        }
    }
}
