using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace Uniflow.Models
{
    public class UserBadge
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public IdentityUser? User { get; set; }

        [Required]
        public int BadgeId { get; set; }

        [ForeignKey("BadgeId")]
        public Badge? Badge { get; set; }

        public DateTime EarnedDate { get; set; } = DateTime.Now;
    }
}
