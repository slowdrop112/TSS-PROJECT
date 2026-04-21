using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Uniflow.Data;
using Uniflow.Models;

namespace Uniflow.Pages.Notes
{
    [Authorize(Roles = "Student,Profesor,Admin")]
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly Uniflow.Services.NotificationService _notificationService;
        private const int DefaultCommentsPageSize = 5;

        public DetailsModel(ApplicationDbContext context, UserManager<IdentityUser> userManager, Uniflow.Services.NotificationService notificationService)
        {
            _context = context;
            _userManager = userManager;
            _notificationService = notificationService;
        }

        public NoteViewModel Note { get; set; } = default!;
        public bool IsOwner { get; set; }
        public bool IsShared { get; set; } // Dacă notița este partajată cu utilizatorul (read-only)
        public bool? UserVote { get; set; } // null = nu a votat, true = upvote, false = downvote
        public List<StudentViewModel> AvailableStudents { get; set; } = new List<StudentViewModel>();
        public List<CommentViewModel> Comments { get; set; } = new List<CommentViewModel>();
        public int TotalCommentsCount { get; set; }
        public int CommentsShown { get; set; }
        public bool HasMoreComments { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? CommentsToShow { get; set; }

        [BindProperty]
        public string? NewCommentContent { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var currentUserId = _userManager.GetUserId(User);
            if (currentUserId == null)
            {
                return RedirectToPage("/Account/Login");
            }

            var note = await _context.Notes
                .Include(n => n.Course)
                .Include(n => n.Student)
                .Include(n => n.Votes)
                .Include(n => n.Comments)
                    .ThenInclude(c => c.Author)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (note == null)
            {
                return NotFound();
            }

            // Verifică dacă utilizatorul este înscris la curs
            var isEnrolled = await _context.CourseEnrollments
                .AnyAsync(e => e.StudentId == currentUserId && e.CourseId == note.CourseId);

            // Verifică dacă notița este partajată cu utilizatorul
            IsShared = await _context.NoteShares
                .AnyAsync(s => s.NoteId == id && s.SharedWithUserId == currentUserId);

            // Verifică dacă utilizatorul este profesorul cursului sau admin
            var isProfessor = User.IsInRole("Profesor") && note.Course != null && note.Course.ProfesorId == currentUserId;
            var isAdmin = User.IsInRole("Admin");

            if (!isEnrolled && note.StudentId != currentUserId && !IsShared && !isProfessor && !isAdmin)
            {
                return Forbid();
            }

            IsOwner = note.StudentId == currentUserId;

            // Verifică votul utilizatorului
            var userVote = await _context.NoteVotes
                .FirstOrDefaultAsync(v => v.NoteId == id && v.UserId == currentUserId);
            UserVote = userVote?.IsUpvote;

            // Get student name from AspNetUsers table
            var studentName = "N/A";
            if (note.StudentId != null)
            {
                var studentData = await _context.UserProfiles
                    .Where(u => u.UserId == note.StudentId)
                    .Select(u => new { u.FirstName, u.LastName })
                    .FirstOrDefaultAsync();
                
                if (studentData != null && !string.IsNullOrWhiteSpace(studentData.FirstName) && !string.IsNullOrWhiteSpace(studentData.LastName))
                {
                    studentName = $"{studentData.FirstName} {studentData.LastName}";
                }
                else
                {
                    studentName = note.Student?.Email ?? "N/A";
                }
            }

            Note = new NoteViewModel
            {
                Id = note.Id,
                Title = note.Title,
                Content = note.Content ?? "",
                CourseTitle = note.Course != null ? note.Course.Title : "N/A",
                StudentName = studentName,
                CreatedDate = note.CreatedDate,
                Status = note.Status,
                Upvotes = note.Votes.Count(v => v.IsUpvote),
                Downvotes = note.Votes.Count(v => !v.IsUpvote),
                Score = note.Votes.Count(v => v.IsUpvote) - note.Votes.Count(v => !v.IsUpvote)
            };

            // Obține studenții disponibili pentru partajare (doar dacă ești proprietar)
            if (IsOwner)
            {
                // Obține ID-urile studenților înscriși la curs
                var enrolledStudentIds = await _context.CourseEnrollments
                    .Where(e => e.CourseId == note.CourseId)
                    .Select(e => e.StudentId)
                    .ToListAsync();

                // Obține toți utilizatorii care sunt studenți și sunt înscriși la curs
                var allUsers = await _userManager.Users.ToListAsync();
                var studentsInRole = await _userManager.GetUsersInRoleAsync("Student");
                
                // Filtrează: studenți înscriși la curs, dar nu utilizatorul curent
                var eligibleStudents = studentsInRole
                    .Where(u => enrolledStudentIds.Contains(u.Id) && u.Id != currentUserId)
                    .ToList();

                // Exclude studenții cu care notița este deja partajată
                var alreadySharedWith = await _context.NoteShares
                    .Where(s => s.NoteId == id)
                    .Select(s => s.SharedWithUserId)
                    .ToListAsync();

                var filteredStudents = eligibleStudents
                    .Where(u => !alreadySharedWith.Contains(u.Id))
                    .ToList();

                // Map to StudentViewModel with proper names from AspNetUsers
                AvailableStudents = new List<StudentViewModel>();
                foreach (var student in filteredStudents)
                {
                    var userData = await _context.UserProfiles
                        .Where(u => u.UserId == student.Id)
                        .Select(u => new { u.FirstName, u.LastName })
                        .FirstOrDefaultAsync();
                    
                    var displayName = "N/A";
                    if (userData != null && !string.IsNullOrWhiteSpace(userData.FirstName) && !string.IsNullOrWhiteSpace(userData.LastName))
                    {
                        displayName = $"{userData.FirstName} {userData.LastName}";
                    }
                    else
                    {
                        displayName = student.Email ?? "N/A";
                    }

                    AvailableStudents.Add(new StudentViewModel
                    {
                        Id = student.Id,
                        DisplayName = displayName
                    });
                }
            }

            // Obține comentariile (doar comentariile principale, fără reply-uri pentru moment)
            TotalCommentsCount = await _context.NoteComments
                .Where(c => c.NoteId == id && c.ParentCommentId == null)
                .CountAsync();

            var requestedToShow = CommentsToShow.GetValueOrDefault(DefaultCommentsPageSize);
            if (requestedToShow <= 0) requestedToShow = DefaultCommentsPageSize;
            var toShow = Math.Min(requestedToShow, TotalCommentsCount);

            var commentsData = await _context.NoteComments
                .Include(c => c.Author)
                .Include(c => c.Replies)
                    .ThenInclude(r => r.Author)
                .Where(c => c.NoteId == id && c.ParentCommentId == null)
                .OrderBy(c => c.CreatedDate)
                .Take(toShow)
                .ToListAsync();

            // Map comments with proper names from AspNetUsers
            Comments = new List<CommentViewModel>();
            foreach (var c in commentsData)
            {
                // Get comment author name
                var commentAuthorName = "N/A";
                if (c.AuthorId != null)
                {
                    var authorData = await _context.UserProfiles
                        .Where(u => u.UserId == c.AuthorId)
                        .Select(u => new { u.FirstName, u.LastName })
                        .FirstOrDefaultAsync();
                    
                    if (authorData != null && !string.IsNullOrWhiteSpace(authorData.FirstName) && !string.IsNullOrWhiteSpace(authorData.LastName))
                    {
                        commentAuthorName = $"{authorData.FirstName} {authorData.LastName}";
                    }
                    else
                    {
                        commentAuthorName = c.Author?.Email ?? "N/A";
                    }
                }

                // Map replies with proper names
                var repliesVM = new List<CommentViewModel>();
                foreach (var r in c.Replies)
                {
                    var replyAuthorName = "N/A";
                    if (r.AuthorId != null)
                    {
                        var replyAuthorData = await _context.UserProfiles
                            .Where(u => u.UserId == r.AuthorId)
                            .Select(u => new { u.FirstName, u.LastName })
                            .FirstOrDefaultAsync();
                        
                        if (replyAuthorData != null && !string.IsNullOrWhiteSpace(replyAuthorData.FirstName) && !string.IsNullOrWhiteSpace(replyAuthorData.LastName))
                        {
                            replyAuthorName = $"{replyAuthorData.FirstName} {replyAuthorData.LastName}";
                        }
                        else
                        {
                            replyAuthorName = r.Author?.Email ?? "N/A";
                        }
                    }

                    repliesVM.Add(new CommentViewModel
                    {
                        Id = r.Id,
                        Content = r.Content,
                        AuthorName = replyAuthorName,
                        CreatedDate = r.CreatedDate,
                        IsAuthor = r.AuthorId == currentUserId
                    });
                }

                Comments.Add(new CommentViewModel
                {
                    Id = c.Id,
                    Content = c.Content,
                    AuthorName = commentAuthorName,
                    CreatedDate = c.CreatedDate,
                    IsAuthor = c.AuthorId == currentUserId,
                    Replies = repliesVM.OrderBy(r => r.CreatedDate).ToList()
                });
            }

            CommentsShown = Comments.Count;
            HasMoreComments = CommentsShown < TotalCommentsCount;

            return Page();
        }

        public async Task<IActionResult> OnPostShareAsync(int noteId, string sharedWithUserId)
        {
            var currentUserId = _userManager.GetUserId(User);
            if (currentUserId == null)
            {
                return RedirectToPage("/Account/Login");
            }

            // Verifică dacă notița există și dacă utilizatorul este proprietar
            var note = await _context.Notes.FindAsync(noteId);
            if (note == null || note.StudentId != currentUserId)
            {
                return Forbid();
            }

            // Verifică dacă notița este deja partajată cu acest utilizator
            var existingShare = await _context.NoteShares
                .FirstOrDefaultAsync(s => s.NoteId == noteId && s.SharedWithUserId == sharedWithUserId);

            if (existingShare != null)
            {
                TempData["ErrorMessage"] = "Notița este deja partajată cu acest student.";
                return RedirectToPage("./Details", new { id = noteId });
            }

            // Creează partajarea
            var share = new NoteShare
            {
                NoteId = noteId,
                OwnerId = currentUserId,
                SharedWithUserId = sharedWithUserId,
                SharedDate = DateTime.Now
            };

            _context.NoteShares.Add(share);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Notița a fost partajată cu succes!";
            return RedirectToPage("./Details", new { id = noteId });
        }

        public async Task<IActionResult> OnPostAddCommentAsync(int noteId)
        {
            var currentUserId = _userManager.GetUserId(User);
            if (currentUserId == null)
            {
                return RedirectToPage("/Account/Login");
            }

            if (string.IsNullOrWhiteSpace(NewCommentContent))
            {
                TempData["ErrorMessage"] = "Comentariul nu poate fi gol.";
                return RedirectToPage("./Details", new { id = noteId });
            }

            // Verifică dacă notița există și dacă utilizatorul are acces
            var note = await _context.Notes
                .Include(n => n.Course)
                .FirstOrDefaultAsync(n => n.Id == noteId);

            if (note == null)
            {
                return NotFound();
            }

            // Verifică dacă utilizatorul este înscris la curs sau dacă notița este partajată
            var isEnrolled = await _context.CourseEnrollments
                .AnyAsync(e => e.StudentId == currentUserId && e.CourseId == note.CourseId);

            var isShared = await _context.NoteShares
                .AnyAsync(s => s.NoteId == noteId && s.SharedWithUserId == currentUserId);

            if (!isEnrolled && note.StudentId != currentUserId && !isShared)
            {
                return Forbid();
            }

            // Creează comentariul
            var comment = new NoteComment
            {
                NoteId = noteId,
                AuthorId = currentUserId,
                Content = NewCommentContent.Trim(),
                CreatedDate = DateTime.Now
            };

            _context.NoteComments.Add(comment);
            await _context.SaveChangesAsync();

            // Notifică autorul notiței când primește un comentariu (dacă nu este el însuși)
            // Notifică autorul notiței când primește un comentariu (dacă nu este el însuși)
            if (note.StudentId != currentUserId)
            {
                var userProfile = await _context.UserProfiles.FirstOrDefaultAsync(p => p.UserId == currentUserId);
                var commenterName = userProfile != null 
                    ? $"{userProfile.FirstName} {userProfile.LastName}" 
                    : (await _userManager.GetUserAsync(User))?.UserName ?? "Un utilizator";
                
                await _notificationService.NotifyNoteReceivedCommentAsync(
                    noteId,
                    note.StudentId,
                    note.Title,
                    commenterName
                );
            }

            TempData["SuccessMessage"] = "Comentariul a fost adăugat cu succes!";
            return RedirectToPage("./Details", new { id = noteId });
        }

        public async Task<IActionResult> OnPostAddReplyAsync(int noteId, int parentCommentId, string replyContent)
        {
            var currentUserId = _userManager.GetUserId(User);
            if (currentUserId == null)
            {
                return RedirectToPage("/Account/Login");
            }

            if (string.IsNullOrWhiteSpace(replyContent))
            {
                TempData["ErrorMessage"] = "Răspunsul nu poate fi gol.";
                return RedirectToPage("./Details", new { id = noteId });
            }

            // Verifică dacă notița și comentariul părinte există
            var note = await _context.Notes
                .Include(n => n.Course)
                .FirstOrDefaultAsync(n => n.Id == noteId);

            if (note == null)
            {
                return NotFound();
            }

            var parentComment = await _context.NoteComments
                .Include(c => c.Author)
                .FirstOrDefaultAsync(c => c.Id == parentCommentId && c.NoteId == noteId);

            if (parentComment == null)
            {
                TempData["ErrorMessage"] = "Comentariul original nu a fost găsit.";
                return RedirectToPage("./Details", new { id = noteId });
            }

            // Verifică dacă utilizatorul are acces la notiță
            var isEnrolled = await _context.CourseEnrollments
                .AnyAsync(e => e.StudentId == currentUserId && e.CourseId == note.CourseId);

            var isShared = await _context.NoteShares
                .AnyAsync(s => s.NoteId == noteId && s.SharedWithUserId == currentUserId);

            if (!isEnrolled && note.StudentId != currentUserId && !isShared)
            {
                return Forbid();
            }

            // Creează reply-ul
            var reply = new NoteComment
            {
                NoteId = noteId,
                ParentCommentId = parentCommentId,
                AuthorId = currentUserId,
                Content = replyContent.Trim(),
                CreatedDate = DateTime.Now
            };

            _context.NoteComments.Add(reply);
            await _context.SaveChangesAsync();

            // Notifică autorul comentariului original (dacă nu este el însuși)
            // Notifică autorul comentariului original (dacă nu este el însuși)
            if (parentComment.AuthorId != currentUserId)
            {
                var userProfile = await _context.UserProfiles.FirstOrDefaultAsync(p => p.UserId == currentUserId);
                var replierName = userProfile != null 
                    ? $"{userProfile.FirstName} {userProfile.LastName}" 
                    : (await _userManager.GetUserAsync(User))?.UserName ?? "Un utilizator";

                // Use the new specific notification method for replies
                await _notificationService.NotifyReplyReceivedAsync(
                    noteId,
                    parentComment.AuthorId,
                    note.Title,
                    replierName
                );
            }

            TempData["SuccessMessage"] = "Răspunsul a fost adăugat cu succes!";
            return RedirectToPage("./Details", new { id = noteId });
        }

        public class NoteViewModel
        {
            public int Id { get; set; }
            public string Title { get; set; } = string.Empty;
            public string Content { get; set; } = string.Empty;
            public string CourseTitle { get; set; } = string.Empty;
            public string StudentName { get; set; } = string.Empty;
            public DateTime CreatedDate { get; set; }
            public string Status { get; set; } = string.Empty;
            public int Upvotes { get; set; }
            public int Downvotes { get; set; }
            public int Score { get; set; }
        }

        public class CommentViewModel
        {
            public int Id { get; set; }
            public string Content { get; set; } = string.Empty;
            public string AuthorName { get; set; } = string.Empty;
            public DateTime CreatedDate { get; set; }
            public bool IsAuthor { get; set; }
            public List<CommentViewModel> Replies { get; set; } = new List<CommentViewModel>();
        }

        public class StudentViewModel
        {
            public string Id { get; set; } = string.Empty;
            public string DisplayName { get; set; } = string.Empty;
        }
    }
}

