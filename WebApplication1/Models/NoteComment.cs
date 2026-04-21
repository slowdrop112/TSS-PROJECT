using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Uniflow.Models
{
    /// <summary>
    /// Model pentru comentarii la notițe
    /// </summary>
    public class NoteComment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int NoteId { get; set; }

        [ForeignKey("NoteId")]
        public Note? Note { get; set; }

        [Required]
        [Display(Name = "Autor")]
        public string AuthorId { get; set; } = string.Empty;

        [ForeignKey("AuthorId")]
        public Microsoft.AspNetCore.Identity.IdentityUser? Author { get; set; }

        [Required]
        [Display(Name = "Comentariu")]
        [StringLength(2000)]
        public string Content { get; set; } = string.Empty;

        [Display(Name = "Data Comentariului")]
        [DataType(DataType.DateTime)]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Comentariu părinte (pentru reply-uri, opțional pentru viitor)
        public int? ParentCommentId { get; set; }

        [ForeignKey("ParentCommentId")]
        public NoteComment? ParentComment { get; set; }

        // Relație cu comentarii copil (reply-uri)
        public ICollection<NoteComment> Replies { get; set; } = new List<NoteComment>();
    }
}

