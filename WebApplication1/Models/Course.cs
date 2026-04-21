using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Uniflow.Models
{
    [Table("Courses")]
    public class Course
    {
        [Key]
        [Column("CourseID")]
        public int Id { get; set; }

        [Required]
        [StringLength(255)]
        [Display(Name = "Titlu")]
        public string Title { get; set; } = string.Empty;

        [Display(Name = "Descriere")]
        public string? Description { get; set; }

        [StringLength(50)]
        public string? Category { get; set; }

        public int? DurationHours { get; set; }

        [Required]
        [Column("DateCreated")]
        [Display(Name = "Data Creării")]
        [DataType(DataType.DateTime)]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [Required]
        public bool IsPublished { get; set; } = true;

        // ProfesorId - va fi adăugat când se va actualiza baza de date
        // Pentru moment, o facem nullable și o ignorăm în query-uri dacă nu există
        [Column("ProfesorId")]
        [Display(Name = "Profesor")]
        public string? ProfesorId { get; set; }

        [ForeignKey("ProfesorId")]
        public Microsoft.AspNetCore.Identity.IdentityUser? Profesor { get; set; }

        // Relație many-to-many cu studenții prin CourseEnrollment
        public ICollection<CourseEnrollment> Enrollments { get; set; } = new List<CourseEnrollment>();
    }
}

