using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using App.Models;

namespace XEDAPVIP.Models
{
    [Table("CartItems")]
    public class CartItem
    {
        public int Id { get; set; }

        public string? UserId { get; set; }

        public int VariantId { get; set; }
        [ForeignKey("VariantId")]
        public ProductVariant? Variant { get; set; }
        [Required]
        public int Quantity { get; set; }
    }
}
