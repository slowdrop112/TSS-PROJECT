using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Uniflow.Models
{
    public class CourseMaterial
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(255)]
        [Display(Name = "Nume Fișier")]
        public string FileName { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        [Display(Name = "Cale Fișier")]
        public string FilePath { get; set; } = string.Empty;

        [Display(Name = "Descriere")]
        [StringLength(500)]
        public string? Description { get; set; }

        [Required]
        public int CourseId { get; set; }

        [ForeignKey("CourseId")]
        public Course? Course { get; set; }

        [Required]
        public string UploadedByUserId { get; set; } = string.Empty;

        [ForeignKey("UploadedByUserId")]
        public Microsoft.AspNetCore.Identity.IdentityUser? UploadedByUser { get; set; }

        [Display(Name = "Data Încărcării")]
        [DataType(DataType.DateTime)]
        public DateTime UploadDate { get; set; } = DateTime.Now;

        [Display(Name = "Dimensiune (bytes)")]
        public long FileSize { get; set; }

        [Display(Name = "Tip MIME")]
        [StringLength(100)]
        public string? ContentType { get; set; }
    }
}




