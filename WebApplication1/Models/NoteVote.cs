using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Uniflow.Models
{
    public class NoteVote
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int NoteId { get; set; }

        [ForeignKey("NoteId")]
        public Note? Note { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public Microsoft.AspNetCore.Identity.IdentityUser? User { get; set; }

        [Required]
        [Display(Name = "Upvote")]
        public bool IsUpvote { get; set; } // true = upvote, false = downvote

        [Display(Name = "Data Votului")]
        [DataType(DataType.DateTime)]
        public DateTime VoteDate { get; set; } = DateTime.Now;

        // Index compus pentru a preveni voturi duplicate
    }
}




