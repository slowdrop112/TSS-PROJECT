using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Uniflow.Models
{
    public class CourseEnrollment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int CourseId { get; set; }

        [ForeignKey("CourseId")]
        public Course? Course { get; set; }

        [Required]
        public string StudentId { get; set; } = string.Empty;

        [ForeignKey("StudentId")]
        public Microsoft.AspNetCore.Identity.IdentityUser? Student { get; set; }

        [Display(Name = "Data Înscrierii")]
        [DataType(DataType.DateTime)]
        public DateTime EnrollmentDate { get; set; } = DateTime.Now;

        // Index compus pentru a preveni înscrieri duplicate
        // (un student nu poate fi înscris de două ori la același curs)
    }
}





