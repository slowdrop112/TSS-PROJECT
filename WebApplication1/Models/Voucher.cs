using System.ComponentModel.DataAnnotations;

namespace Uniflow.Models
{
    /// <summary>
    /// Voucher template - tipul de voucher care poate fi acordat studenților
    /// </summary>
    public class Voucher
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        [Display(Name = "Titlu")]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(1000)]
        [Display(Name = "Descriere")]
        public string Description { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        [Display(Name = "Partener")]
        public string PartnerName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        [Display(Name = "Tip Reducere")]
        public string DiscountType { get; set; } = string.Empty; // "Percentage", "FixedAmount", "FreeItem"

        [Required]
        [StringLength(50)]
        [Display(Name = "Valoare Reducere")]
        public string DiscountValue { get; set; } = string.Empty; // "20%", "15 lei", "1 cafea gratis"

        [Display(Name = "Nivel Necesar")]
        public int RequiredLevel { get; set; } = 5; // La ce nivel se acordă acest voucher

        [Display(Name = "Zile Valabilitate")]
        public int ValidityDays { get; set; } = 30; // Câte zile este valabil voucherul după acordare

        [StringLength(500)]
        [Display(Name = "Icon URL")]
        public string? IconUrl { get; set; }

        [Display(Name = "Activ")]
        public bool IsActive { get; set; } = true;
    }
}
