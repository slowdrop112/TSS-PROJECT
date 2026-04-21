using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Uniflow.Models
{
    public class CourseAnnouncement
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(500)]
        [Display(Name = "Titlu")]
        public string Title { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Conținut")]
        public string Content { get; set; } = string.Empty;

        [Required]
        public int CourseId { get; set; }

        [ForeignKey("CourseId")]
        public Course? Course { get; set; }

        [Required]
        public string PostedByUserId { get; set; } = string.Empty;

        [ForeignKey("PostedByUserId")]
        public Microsoft.AspNetCore.Identity.IdentityUser? PostedByUser { get; set; }

        [Display(Name = "Data Postării")]
        [DataType(DataType.DateTime)]
        public DateTime PostedDate { get; set; } = DateTime.Now;

        [Display(Name = "Important")]
        public bool IsImportant { get; set; } = false;
    }
}

