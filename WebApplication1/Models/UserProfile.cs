using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Uniflow.Models
{
    public class UserProfile
    {
        [Key]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public Microsoft.AspNetCore.Identity.IdentityUser? User { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Nume")]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        [Display(Name = "Prenume")]
        public string FirstName { get; set; } = string.Empty;

        [Display(Name = "Puncte XP")]
        public int XP { get; set; } = 0;

        [NotMapped]
        public string FullName => $"{FirstName} {LastName}";
    }
}

