using System.ComponentModel.DataAnnotations;

namespace Uniflow.Models
{
    public class Badge
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string IconClass { get; set; } = string.Empty; // FontAwesome class, e.g., "fa-solid fa-trophy"

        [StringLength(20)]
        public string ColorClass { get; set; } = "primary"; // Bootstrap color: warning, primary, secondary, etc.

        [Required]
        [StringLength(50)]
        public string Type { get; set; } = string.Empty; // Unique identifier for logic: "Rank1", "MostLikes", etc.
    }
}
