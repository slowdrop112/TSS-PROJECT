using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Uniflow.Models
{
    public class Note
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(255)]
        [Display(Name = "Titlu")]
        public string Title { get; set; } = string.Empty;

        [Display(Name = "Conținut")]
        public string? Content { get; set; }

        [Required]
        public int CourseId { get; set; }

        [ForeignKey("CourseId")]
        public Course? Course { get; set; }

        [Required]
        public string StudentId { get; set; } = string.Empty;

        [ForeignKey("StudentId")]
        public Microsoft.AspNetCore.Identity.IdentityUser? Student { get; set; }

        [Display(Name = "Data Publicării")]
        [DataType(DataType.DateTime)]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [Display(Name = "Status")]
        [StringLength(20)]
        public string Status { get; set; } = "Pending"; // "Pending", "Approved", "Rejected"

        public string? ValidatedByUserId { get; set; }

        [ForeignKey("ValidatedByUserId")]
        public Microsoft.AspNetCore.Identity.IdentityUser? ValidatedByUser { get; set; }

        [Display(Name = "Data Validării")]
        [DataType(DataType.DateTime)]
        public DateTime? ValidationDate { get; set; }

        /// <summary>
        /// XP deja acordat pentru voturi (pentru a evita dublarea)
        /// </summary>
        [Display(Name = "XP Acordat pentru Voturi")]
        public int XPAwardedForVotes { get; set; } = 0;

        // Relație cu voturi
        public ICollection<NoteVote> Votes { get; set; } = new List<NoteVote>();

        // Relație cu partajări
        public ICollection<NoteShare> Shares { get; set; } = new List<NoteShare>();

        // Relație cu comentarii
        public ICollection<NoteComment> Comments { get; set; } = new List<NoteComment>();

        [NotMapped]
        public int Upvotes => Votes?.Count(v => v.IsUpvote) ?? 0;

        [NotMapped]
        public int Downvotes => Votes?.Count(v => !v.IsUpvote) ?? 0;

        [NotMapped]
        public int Score => Upvotes - Downvotes; // Scor Reddit-like
    }
}



