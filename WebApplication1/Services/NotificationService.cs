using Microsoft.EntityFrameworkCore;
using Uniflow.Data;
using Uniflow.Models;

namespace Uniflow.Services
{
    /// <summary>
    /// Serviciu pentru gestionarea notificărilor utilizatorilor
    /// </summary>
    public class NotificationService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(ApplicationDbContext context, ILogger<NotificationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Creează o notificare pentru un utilizator
        /// </summary>
        private async Task CreateNotificationAsync(
            string userId, 
            string title, 
            string message, 
            string type = "info", 
            string? linkUrl = null)
        {
            try
            {
                var notification = new UserNotification
                {
                    UserId = userId,
                    Title = title,
                    Message = message,
                    Type = type,
                    LinkUrl = linkUrl,
                    IsRead = false,
                    CreatedDate = DateTime.Now
                };

                _context.UserNotifications.Add(notification);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Notificare creată pentru utilizatorul {userId}: {title}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Eroare la crearea notificării pentru utilizatorul {userId}");
                // Nu aruncăm excepția pentru a nu bloca fluxul principal
            }
        }

        /// <summary>
        /// Notifică studentul când notița sa este aprobată
        /// </summary>
        public async Task NotifyNoteApprovedAsync(int noteId, string studentId, string noteTitle, string courseTitle)
        {
            await CreateNotificationAsync(
                studentId,
                "Notiță Aprobată ✅",
                $"Notița ta \"{noteTitle}\" pentru cursul \"{courseTitle}\" a fost aprobată!",
                "success",
                $"/Notes/Details/{noteId}"
            );
        }

        /// <summary>
        /// Notifică studentul când notița sa este respinsă
        /// </summary>
        public async Task NotifyNoteRejectedAsync(int noteId, string studentId, string noteTitle, string courseTitle)
        {
            await CreateNotificationAsync(
                studentId,
                "Notiță Respinsă ❌",
                $"Notița ta \"{noteTitle}\" pentru cursul \"{courseTitle}\" a fost respinsă.",
                "warning",
                $"/Notes/Details/{noteId}"
            );
        }

        /// <summary>
        /// Notifică autorul notiței când primește un vot
        /// </summary>
        public async Task NotifyNoteReceivedVoteAsync(int noteId, string authorId, string noteTitle, bool isUpvote)
        {
            var voteType = isUpvote ? "upvote 👍" : "downvote 👎";
            var type = isUpvote ? "success" : "info";

            await CreateNotificationAsync(
                authorId,
                $"Ai primit un {voteType}",
                $"Notița ta \"{noteTitle}\" a primit un {voteType}!",
                type,
                $"/Notes/Details/{noteId}"
            );
        }

        /// <summary>
        /// Notifică autorul notiței când primește un comentariu
        /// </summary>
        public async Task NotifyNoteReceivedCommentAsync(int noteId, string authorId, string noteTitle, string commenterName)
        {
            await CreateNotificationAsync(
                authorId,
                "Comentariu Nou 💬",
                $"{commenterName} a comentat la notița ta \"{noteTitle}\".",
                "info",
                $"/Notes/Details/{noteId}"
            );
        }

        /// <summary>
        /// Notifică toți studenții înscriși la un curs despre un anunț nou
        /// </summary>
        public async Task NotifyStudentsOfAnnouncementAsync(int courseId, string announceTitle, string teacherName)
        {
            try
            {
                // Obține cursul
                var course = await _context.Courses.FindAsync(courseId);
                if (course == null) return;

                // Obține toți studenții înscriși la curs
                var enrolledStudentIds = await _context.CourseEnrollments
                    .Where(e => e.CourseId == courseId)
                    .Select(e => e.StudentId)
                    .ToListAsync();

                // Creează notificări pentru toți studenții
                foreach (var studentId in enrolledStudentIds)
                {
                    await CreateNotificationAsync(
                        studentId,
                        "Anunț Nou 📢",
                        $"Profesorul {teacherName} a postat un anunț nou la cursul \"{course.Title}\": {announceTitle}",
                        "info",
                        $"/Courses/Details/{courseId}?tab=announcements"
                    );
                }

                _logger.LogInformation($"Notificări de anunț trimise către {enrolledStudentIds.Count} studenți pentru cursul {course.Title}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Eroare la trimiterea notificărilor de anunț pentru cursul {courseId}");
            }
        }

        /// <summary>
        /// Notifică toți studenții înscriși la un curs despre un material nou
        /// </summary>
        public async Task NotifyStudentsOfMaterialAsync(int courseId, string materialTitle, string teacherName)
        {
            try
            {
                // Obține cursul
                var course = await _context.Courses.FindAsync(courseId);
                if (course == null) return;

                // Obține toți studenții înscriși la curs
                var enrolledStudentIds = await _context.CourseEnrollments
                    .Where(e => e.CourseId == courseId)
                    .Select(e => e.StudentId)
                    .ToListAsync();

                // Creează notificări pentru toți studenții
                foreach (var studentId in enrolledStudentIds)
                {
                    await CreateNotificationAsync(
                        studentId,
                        "Material Nou 📚",
                        $"Profesorul {teacherName} a încărcat un material nou \"{materialTitle}\" la cursul \"{course.Title}\".",
                        "info",
                        $"/Courses/Details/{courseId}?tab=materials"
                    );
                }

                _logger.LogInformation($"Notificări de material trimise către {enrolledStudentIds.Count} studenți pentru cursul {course.Title}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Eroare la trimiterea notificărilor de material pentru cursul {courseId}");
            }
        }

        /// <summary>
        /// Notifică profesorul când un student postează o notiță
        /// </summary>
        public async Task NotifyProfessorOfNewNoteAsync(int courseId, int noteId, string noteTitle, string studentName)
        {
            try
            {
                // Obține cursul
                var course = await _context.Courses.FindAsync(courseId);
                
                if (course == null)
                {
                    _logger.LogWarning($"Cursul {courseId} nu a fost găsit pentru notificare notiță nouă");
                    return;
                }
                
                if (string.IsNullOrEmpty(course.ProfesorId))
                {
                    _logger.LogWarning($"Cursul '{course.Title}' (ID: {courseId}) nu are ProfesorId setat!");
                    return;
                }

                await CreateNotificationAsync(
                    course.ProfesorId,
                    "Notiță Nouă pentru Validare 📝",
                    $"{studentName} a postat o notiță nouă \"{noteTitle}\" la cursul \"{course.Title}\" care necesită validare.",
                    "info",
                    $"/Notes/Validate?tab=pending"
                );
                
                _logger.LogInformation($"Notificare trimisă către profesorul {course.ProfesorId} pentru notița nouă {noteId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Eroare la notificarea profesorului pentru notița nouă {noteId}");
            }
        }

        /// <summary>
        /// Notifică profesorul când un student se înscrie la cursul său
        /// </summary>
        public async Task NotifyProfessorOfEnrollmentAsync(int courseId, string studentName)
        {
            try
            {
                // Obține cursul
                var course = await _context.Courses.FindAsync(courseId);
                
                if (course == null)
                {
                    _logger.LogWarning($"Cursul {courseId} nu a fost găsit pentru notificare înscriere");
                    return;
                }
                
                if (string.IsNullOrEmpty(course.ProfesorId))
                {
                    _logger.LogWarning($"Cursul '{course.Title}' (ID: {courseId}) nu are ProfesorId setat!");
                    return;
                }

                await CreateNotificationAsync(
                    course.ProfesorId,
                    "Student Nou Înscris 🎓",
                    $"{studentName} s-a înscris la cursul tău \"{course.Title}\".",
                    "success",
                    $"/Courses/Details/{courseId}"
                );
                
                _logger.LogInformation($"Notificare trimisă către profesorul {course.ProfesorId} pentru înscriere la cursul {courseId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Eroare la notificarea profesorului pentru înscriere la cursul {courseId}");
            }
        }
        /// <summary>
        /// Notifică toți utilizatorii platformei (Global)
        /// </summary>
        public async Task NotifyAllUsersAsync(string title, string message, string type = "info", string? linkUrl = null)
        {
            try
            {
                var allUserIds = await _context.Users.Select(u => u.Id).ToListAsync();
                
                // Group notifications into a single transaction for efficiency if needed, 
                // but for now, we follow the established pattern.
                foreach (var userId in allUserIds)
                {
                    var notification = new UserNotification
                    {
                        UserId = userId,
                        Title = title,
                        Message = message,
                        Type = type,
                        LinkUrl = linkUrl,
                        IsRead = false,
                        CreatedDate = DateTime.Now
                    };
                    _context.UserNotifications.Add(notification);
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation($"Notificare globală '{title}' trimisă către {allUserIds.Count} utilizatori.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Eroare la trimiterea notificării globale.");
            }
        }
        /// <summary>
        /// Notifică un utilizator că a primit un răspuns la comentariul său
        /// </summary>
        public async Task NotifyReplyReceivedAsync(int noteId, string targetUserId, string noteTitle, string replierName)
        {
            await CreateNotificationAsync(
                targetUserId,
                "Răspuns Nou ↩️",
                $"{replierName} a răspuns la comentariul tău de la notița \"{noteTitle}\".",
                "info",
                $"/Notes/Details/{noteId}"
            );
        }

        /// <summary>
        /// Notifică utilizatorul că a primit un voucher pentru atingerea unui nivel milestone
        /// </summary>
        public async Task NotifyVoucherAwardedAsync(string userId, string voucherTitle, string partnerName, string code, int level)
        {
            await CreateNotificationAsync(
                userId,
                $"🎉 Felicitări! Ai atins Nivelul {level}",
                $"Ai primit un voucher: {voucherTitle} de la {partnerName}! Codul tău: {code}",
                "success",
                "/Vouchers"
            );
        }
    }
}
