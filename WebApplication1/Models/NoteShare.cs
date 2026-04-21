using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Uniflow.Models
{
    /// <summary>
    /// Model pentru partajarea notițelor între studenți
    /// </summary>
    public class NoteShare
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int NoteId { get; set; }

        [ForeignKey("NoteId")]
        public Note? Note { get; set; }

        [Required]
        [Display(Name = "Proprietar Notiță")]
        public string OwnerId { get; set; } = string.Empty; // Studentul care deține notița

        [ForeignKey("OwnerId")]
        public Microsoft.AspNetCore.Identity.IdentityUser? Owner { get; set; }

        [Required]
        [Display(Name = "Partajat cu")]
        public string SharedWithUserId { get; set; } = string.Empty; // Studentul cu care se partajează

        [ForeignKey("SharedWithUserId")]
        public Microsoft.AspNetCore.Identity.IdentityUser? SharedWithUser { get; set; }

        [Display(Name = "Data Partajării")]
        [DataType(DataType.DateTime)]
        public DateTime SharedDate { get; set; } = DateTime.Now;
    }
}


