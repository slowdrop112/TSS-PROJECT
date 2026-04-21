using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Uniflow.Data;
using Uniflow.Models;

namespace Uniflow.Pages.Notes
{
    [Authorize(Roles = "Student")]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public IndexModel(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public IList<NoteViewModel> MyNotes { get; set; } = new List<NoteViewModel>();
        public IList<NoteViewModel> SharedWithMe { get; set; } = new List<NoteViewModel>();
        public IList<NoteViewModel> AllNotes { get; set; } = new List<NoteViewModel>();

        [BindProperty(SupportsGet = true)]
        public string? Tab { get; set; } = "my"; // "my", "shared", "all"

        [BindProperty(SupportsGet = true)]
        public string? SearchString { get; set; }

        public async Task OnGetAsync()
        {
            var currentUserId = _userManager.GetUserId(User);
            if (currentUserId == null) return;

            // Notițele mele
            var myNotesQuery = _context.Notes
                .Include(n => n.Course)
                .Include(n => n.Student)
                .Include(n => n.Votes)
                .Where(n => n.StudentId == currentUserId);

            if (!string.IsNullOrEmpty(SearchString))
            {
                myNotesQuery = myNotesQuery.Where(n => 
                    n.Title.Contains(SearchString) || 
                    (n.Content != null && n.Content.Contains(SearchString)));
            }

            MyNotes = await myNotesQuery
                .OrderByDescending(n => n.CreatedDate)
                .Select(n => new NoteViewModel
                {
                    Id = n.Id,
                    Title = n.Title,
                    Content = n.Content ?? "",
                    CourseTitle = n.Course != null ? n.Course.Title : "N/A",
                    StudentName = n.Student != null ? n.Student.UserName ?? n.Student.Email ?? "N/A" : "N/A",
                    CreatedDate = n.CreatedDate,
                    Status = n.Status,
                    Upvotes = n.Votes.Count(v => v.IsUpvote),
                    Downvotes = n.Votes.Count(v => !v.IsUpvote),
                    Score = n.Votes.Count(v => v.IsUpvote) - n.Votes.Count(v => !v.IsUpvote)
                })
                .ToListAsync();

            // Notițele partajate cu mine
            var sharedNotesQuery = _context.NoteShares
                .Include(s => s.Note)
                    .ThenInclude(n => n!.Course)
                .Include(s => s.Note)
                    .ThenInclude(n => n!.Student)
                .Include(s => s.Note)
                    .ThenInclude(n => n!.Votes)
                .Include(s => s.Owner)
                .Where(s => s.SharedWithUserId == currentUserId);

            if (!string.IsNullOrEmpty(SearchString))
            {
                sharedNotesQuery = sharedNotesQuery.Where(s => 
                    s.Note != null && (
                        s.Note.Title.Contains(SearchString) || 
                        (s.Note.Content != null && s.Note.Content.Contains(SearchString))
                    ));
            }

            SharedWithMe = await sharedNotesQuery
                .Where(s => s.Note != null)
                .OrderByDescending(s => s.SharedDate)
                .Select(s => new NoteViewModel
                {
                    Id = s.Note!.Id,
                    Title = s.Note.Title,
                    Content = s.Note.Content ?? "",
                    CourseTitle = s.Note.Course != null ? s.Note.Course.Title : "N/A",
                    StudentName = s.Note.Student != null ? s.Note.Student.UserName ?? s.Note.Student.Email ?? "N/A" : "N/A",
                    OwnerName = s.Owner != null ? s.Owner.UserName ?? s.Owner.Email ?? "N/A" : "N/A",
                    CreatedDate = s.Note.CreatedDate,
                    Status = s.Note.Status,
                    Upvotes = s.Note.Votes.Count(v => v.IsUpvote),
                    Downvotes = s.Note.Votes.Count(v => !v.IsUpvote),
                    Score = s.Note.Votes.Count(v => v.IsUpvote) - s.Note.Votes.Count(v => !v.IsUpvote),
                    IsShared = true
                })
                .ToListAsync();

            // Toate notițele (doar pentru cursurile la care sunt înscris)
            var enrolledCourseIds = await _context.CourseEnrollments
                .Where(e => e.StudentId == currentUserId)
                .Select(e => e.CourseId)
                .ToListAsync();

            var allNotesQuery = _context.Notes
                .Include(n => n.Course)
                .Include(n => n.Student)
                .Include(n => n.Votes)
                .Where(n => enrolledCourseIds.Contains(n.CourseId) && n.StudentId != currentUserId);

            if (!string.IsNullOrEmpty(SearchString))
            {
                allNotesQuery = allNotesQuery.Where(n => 
                    n.Title.Contains(SearchString) || 
                    (n.Content != null && n.Content.Contains(SearchString)));
            }

            AllNotes = await allNotesQuery
                .OrderByDescending(n => n.Votes.Count(v => v.IsUpvote) - n.Votes.Count(v => !v.IsUpvote)) // Sortare după scor
                .ThenByDescending(n => n.CreatedDate)
                .Select(n => new NoteViewModel
                {
                    Id = n.Id,
                    Title = n.Title,
                    Content = n.Content ?? "",
                    CourseTitle = n.Course != null ? n.Course.Title : "N/A",
                    StudentName = n.Student != null ? n.Student.UserName ?? n.Student.Email ?? "N/A" : "N/A",
                    CreatedDate = n.CreatedDate,
                    Status = n.Status,
                    Upvotes = n.Votes.Count(v => v.IsUpvote),
                    Downvotes = n.Votes.Count(v => !v.IsUpvote),
                    Score = n.Votes.Count(v => v.IsUpvote) - n.Votes.Count(v => !v.IsUpvote)
                })
                .ToListAsync();
        }

        public class NoteViewModel
        {
            public int Id { get; set; }
            public string Title { get; set; } = string.Empty;
            public string Content { get; set; } = string.Empty;
            public string CourseTitle { get; set; } = string.Empty;
            public string StudentName { get; set; } = string.Empty;
            public string? OwnerName { get; set; }
            public DateTime CreatedDate { get; set; }
            public string Status { get; set; } = string.Empty;
            public int Upvotes { get; set; }
            public int Downvotes { get; set; }
            public int Score { get; set; }
            public bool IsShared { get; set; }
        }
    }
}


