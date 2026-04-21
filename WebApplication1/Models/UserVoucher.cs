using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace Uniflow.Models
{
    /// <summary>
    /// Voucher acordat unui utilizator specific
    /// </summary>
    public class UserVoucher
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public IdentityUser? User { get; set; }

        [Required]
        public int VoucherId { get; set; }

        [ForeignKey("VoucherId")]
        public Voucher? Voucher { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Cod Voucher")]
        public string Code { get; set; } = string.Empty; // Cod unic gen "UNI-5A3B-9C2D"

        [Display(Name = "Data Acordare")]
        public DateTime AwardedDate { get; set; } = DateTime.Now;

        [Display(Name = "Data Expirare")]
        public DateTime ExpiryDate { get; set; }

        [Display(Name = "Folosit")]
        public bool IsRedeemed { get; set; } = false;

        [Display(Name = "Data Folosire")]
        public DateTime? RedeemedDate { get; set; }

        [NotMapped]
        public bool IsExpired => DateTime.Now > ExpiryDate;

        [NotMapped]
        public bool IsActive => !IsRedeemed && !IsExpired;
    }
}
