using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Uniflow.Models
{
    public class RoleRequest
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public Microsoft.AspNetCore.Identity.IdentityUser? User { get; set; }

        [Required]
        public string RequestedRole { get; set; } = string.Empty; // "Profesor"

        [Required]
        public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected

        public DateTime RequestDate { get; set; } = DateTime.Now;

        public DateTime? ProcessedDate { get; set; }

        public string? ProcessedByUserId { get; set; } // Admin care a procesat cererea

        [ForeignKey("ProcessedByUserId")]
        public Microsoft.AspNetCore.Identity.IdentityUser? ProcessedByUser { get; set; }
    }
}





