using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Uniflow.Data;
using Uniflow.Models;

namespace Uniflow.Pages.Courses
{
    [Authorize(Roles = "Student,Profesor,Admin")]
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly Uniflow.Services.GamificationService _gamificationService;
        private readonly Uniflow.Services.NotificationService _notificationService;
        private readonly Uniflow.Services.BadgeService _badgeService;

        public DetailsModel(ApplicationDbContext context, UserManager<IdentityUser> userManager, Uniflow.Services.GamificationService gamificationService, Uniflow.Services.NotificationService notificationService, Uniflow.Services.BadgeService badgeService)
        {
            _context = context;
            _userManager = userManager;
            _gamificationService = gamificationService;
            _notificationService = notificationService;
            _badgeService = badgeService;
        }

        public Course? Course { get; set; }
        public bool IsEnrolled { get; set; }
        public string? ProfesorEmail { get; set; }
        public bool IsOwner { get; set; }
        public List<CourseMaterial> Materials { get; set; } = new();
        public List<ApprovedNoteViewModel> ApprovedNotes { get; set; } = new();
        public List<AnnouncementViewModel> Announcements { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? Tab { get; set; } = "announcements"; // "announcements", "materials" sau "notes"

        public class AnnouncementViewModel
        {
            public int Id { get; set; }
            public string Title { get; set; } = string.Empty;
            public string Content { get; set; } = string.Empty;
            public string AuthorName { get; set; } = string.Empty;
            public DateTime PostedDate { get; set; }
            public bool IsImportant { get; set; }
        }

        public class ApprovedNoteViewModel
        {
            public int Id { get; set; }
            public string Title { get; set; } = string.Empty;
            public string Content { get; set; } = string.Empty;
            public string StudentName { get; set; } = string.Empty;
            public string? StudentId { get; set; } // Added for profile link
            public bool IsMyNote { get; set; } // Added to hide vote buttons
            public DateTime CreatedDate { get; set; }
            public DateTime? ValidationDate { get; set; }
            public int Upvotes { get; set; }
            public int Downvotes { get; set; }
            public int Score { get; set; }
            public bool? UserVote { get; set; } // null = nu a votat, true = upvote, false = downvote
        }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Course = await _context.Courses
                .Include(c => c.Profesor)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (Course == null)
            {
                return NotFound();
            }


            // Obține numele profesorului din UserProfile
            if (Course.Profesor != null)
            {
                var profesorProfile = await _context.UserProfiles
                    .FirstOrDefaultAsync(p => p.UserId == Course.ProfesorId);
                ProfesorEmail = profesorProfile != null ? profesorProfile.FullName : Course.Profesor.UserName ?? Course.Profesor.Email ?? "N/A";
            }
            else
            {
                ProfesorEmail = "N/A";
            }


            // Verificăm dacă studentul este deja înscris
            var currentUserId = _userManager.GetUserId(User);
            IsEnrolled = await _context.CourseEnrollments
                .AnyAsync(e => e.CourseId == Course.Id && e.StudentId == currentUserId);

            // Verificăm dacă profesorul este proprietarul cursului (folosind ProfesorId)
            if (!string.IsNullOrEmpty(Course.ProfesorId) && !string.IsNullOrEmpty(currentUserId))
            {
                IsOwner = Course.ProfesorId == currentUserId && (User.IsInRole("Profesor") || User.IsInRole("Admin"));
            }

            // Obține materialele cursului (doar dacă utilizatorul este înscris sau este profesor/admin)
            if (IsEnrolled || IsOwner || User.IsInRole("Admin") || User.IsInRole("Profesor"))
            {
                Materials = await _context.CourseMaterials
                    .Include(m => m.UploadedByUser)
                    .Where(m => m.CourseId == Course.Id)
                    .OrderByDescending(m => m.UploadDate)
                    .ToListAsync();

                // Obține notițele validate pentru acest curs
                var notesData = await _context.Notes
                    .Include(n => n.Student)
                    .Include(n => n.Votes)
                    .Where(n => n.CourseId == Course.Id && n.Status == "Approved")
                    .OrderByDescending(n => n.ValidationDate ?? n.CreatedDate)
                    .ToListAsync();

                ApprovedNotes = new List<ApprovedNoteViewModel>();
                foreach (var n in notesData)
                {
                    var studentName = "N/A";
                    if (!string.IsNullOrEmpty(n.StudentId))
                    {
                        var profile = await _context.UserProfiles
                            .Where(p => p.UserId == n.StudentId)
                            .Select(p => new { p.FirstName, p.LastName })
                            .FirstOrDefaultAsync();
                        
                        if (profile != null && !string.IsNullOrWhiteSpace(profile.FirstName) && !string.IsNullOrWhiteSpace(profile.LastName))
                        {
                            studentName = $"{profile.FirstName} {profile.LastName}";
                        }
                        else
                        {
                            studentName = n.Student?.Email ?? "N/A";
                        }
                    }

                    // Check user vote
                    var userVote = n.Votes.FirstOrDefault(v => v.UserId == currentUserId);

                    ApprovedNotes.Add(new ApprovedNoteViewModel
                    {
                        Id = n.Id,
                        Title = n.Title,
                        Content = n.Content ?? "",
                        StudentName = studentName,
                        StudentId = n.StudentId, // Assign ID
                        IsMyNote = n.StudentId == currentUserId,
                        CreatedDate = n.CreatedDate,
                        ValidationDate = n.ValidationDate,
                        Upvotes = n.Votes.Count(v => v.IsUpvote),
                        Downvotes = n.Votes.Count(v => !v.IsUpvote),
                        Score = n.Votes.Count(v => v.IsUpvote) - n.Votes.Count(v => !v.IsUpvote),
                        UserVote = userVote?.IsUpvote
                    });
                }

                // Obține anunțurile pentru acest curs cu numele autorilor
                var announcements = await _context.CourseAnnouncements
                    .Include(a => a.PostedByUser)
                    .Where(a => a.CourseId == Course.Id)
                    .OrderByDescending(a => a.IsImportant)
                    .ThenByDescending(a => a.PostedDate)
                    .ToListAsync();

                // Mapare la ViewModel cu numele complet
                Announcements = new List<AnnouncementViewModel>();
                foreach (var announcement in announcements)
                {
                    var authorName = "N/A";
                    if (announcement.PostedByUser != null)
                    {
                        var profile = await _context.UserProfiles
                            .FirstOrDefaultAsync(p => p.UserId == announcement.PostedByUserId);
                        authorName = profile != null ? profile.FullName : announcement.PostedByUser.UserName ?? announcement.PostedByUser.Email ?? "N/A";
                    }

                    Announcements.Add(new AnnouncementViewModel
                    {
                        Id = announcement.Id,
                        Title = announcement.Title,
                        Content = announcement.Content,
                        AuthorName = authorName,
                        PostedDate = announcement.PostedDate,
                        IsImportant = announcement.IsImportant
                    });
                }
            }

            return Page();
        }

        public async Task<IActionResult> OnPostVoteAsync(int noteId, string voteType)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Unauthorized();
            }

            var note = await _context.Notes
                .Include(n => n.Votes)
                .FirstOrDefaultAsync(n => n.Id == noteId);

            if (note == null)
            {
                return NotFound();
            }

            // Prevent self-voting
            if (note.StudentId == currentUser.Id)
            {
                return Forbid();
            }

            var existingVote = note.Votes.FirstOrDefault(v => v.UserId == currentUser.Id);
            bool isUpvote = voteType == "upvote";
            bool voteChanged = false;

            if (existingVote != null)
            {
                if (existingVote.IsUpvote == isUpvote)
                {
                    // Remove vote if same type
                    _context.NoteVotes.Remove(existingVote);
                    voteChanged = true;
                }
                else
                {
                    // Change vote type
                    existingVote.IsUpvote = isUpvote;
                    voteChanged = true;
                }
            }
            else
            {
                // New vote
                var vote = new NoteVote
                {
                    NoteId = noteId,
                    UserId = currentUser.Id,
                    IsUpvote = isUpvote,
                    VoteDate = DateTime.Now
                };
                _context.NoteVotes.Add(vote);
                voteChanged = true;
            }

            await _context.SaveChangesAsync();

            if (voteChanged)
            {
                // Apply Gamification Rules (Tiered XP, Milestones)
                await _gamificationService.AwardXPFromNoteVotesAsync(note.Id, note.StudentId, note.Title);
                
                // Notify author about the vote (if not self-vote)
                if (note.StudentId != currentUser.Id)
                {
                    await _notificationService.NotifyNoteReceivedVoteAsync(note.Id, note.StudentId, note.Title, isUpvote);
                }

                // Check if author earned any permanent badges (e.g. Popular Note)
                await _badgeService.CheckPersistentBadgesAsync(note.StudentId);
            }

            // Reload note to get fresh counts
             var updatedNote = await _context.Notes
                .Include(n => n.Votes)
                .FirstOrDefaultAsync(n => n.Id == noteId);

            var upvotes = updatedNote.Votes.Count(v => v.IsUpvote);
            var downvotes = updatedNote.Votes.Count(v => !v.IsUpvote);
            var userVote = updatedNote.Votes.FirstOrDefault(v => v.UserId == currentUser.Id)?.IsUpvote;

            return new JsonResult(new { upvotes, downvotes, userVote });
        }

        public async Task<IActionResult> OnPostEnrollAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var currentUserId = _userManager.GetUserId(User);
            
            // Verificăm dacă studentul este deja înscris
            var alreadyEnrolled = await _context.CourseEnrollments
                .AnyAsync(e => e.CourseId == id && e.StudentId == currentUserId);

            if (alreadyEnrolled)
            {
                TempData["ErrorMessage"] = "Ești deja înscris la acest curs!";
                return RedirectToPage("./Details", new { id });
            }

            // Verificăm dacă cursul există
            var course = await _context.Courses.FindAsync(id);
            if (course == null)
            {
                return NotFound();
            }

            // Creăm înscrierea
            var enrollment = new CourseEnrollment
            {
                CourseId = course.Id,
                StudentId = currentUserId!,
                EnrollmentDate = DateTime.Now
            };

            _context.CourseEnrollments.Add(enrollment);
            await _context.SaveChangesAsync();

            // Acordă XP pentru înscriere la curs
            await _gamificationService.AwardXPAsync(currentUserId!, Uniflow.Services.GamificationService.XP_ENROLL_COURSE, $"Înscriere la curs: {course.Title}");

            // Notifică profesorul că un student s-a înscris
            var student = await _userManager.GetUserAsync(User);
            var studentName = student?.UserName ?? student?.Email ?? "Un student";
            
            await _notificationService.NotifyProfessorOfEnrollmentAsync(course.Id, studentName);

            TempData["SuccessMessage"] = $"Te-ai înscris cu succes la curs! Ai primit {Uniflow.Services.GamificationService.XP_ENROLL_COURSE} XP!";
            return RedirectToPage("./Details", new { id });
        }
    }
}

