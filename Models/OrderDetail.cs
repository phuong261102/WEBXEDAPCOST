using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace App.Models
{
    [Table("OrderDetails")]
    public class OrderDetail
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int? OrderId { get; set; }
        [ForeignKey("OrderId")]
        public Order? Order { get; set; }

        [Required]
        public int? VariantId { get; set; }
        [ForeignKey("VariantId")]
        public ProductVariant? Variant { get; set; }

        [Required]
        [StringLength(100)]
        public string ProductName { get; set; }

        [StringLength(500)]
        public string ProductDescription { get; set; }

        [StringLength(500)]
        public string ProductImage { get; set; }

        [Required]
        public int Quantity { get; set; }

        [Required]
        [DataType(DataType.Currency)]
        public double UnitPrice { get; set; }

        [NotMapped]
        public double TotalPrice => Quantity * UnitPrice;
    }
}
