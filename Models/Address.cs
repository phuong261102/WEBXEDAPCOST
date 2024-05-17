using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace App.Models
{
    public class Address
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [Column(TypeName = "nvarchar(256)")]
        public string StreetNumber { get; set; }

        [Required]
        [Column(TypeName = "nvarchar(128)")]
        public string SelectedWard { get; set; }

        [Required]
        [Column(TypeName = "nvarchar(128)")]
        public string SelectedDistrict { get; set; }

        [Required]
        [Column(TypeName = "nvarchar(128)")]
        public string SelectedProvince { get; set; }

        // Add other properties as needed
        public bool isDefault { get; set; } // Add this property

        // Foreign key for User (if needed)
        public string UserId { get; set; }
        public AppUser User { get; set; }
    }
}
