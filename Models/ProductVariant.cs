using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace App.Models
{
    [Table("ProductVariants")]
    public class ProductVariant
    {
        [Key]
        public int Id { get; set; }

        public int ProductId { get; set; }

        [ForeignKey("ProductId")]
        public Product Product { get; set; }

        [Required(ErrorMessage = "Màu sản phẩm là bắt buộc")]
        [Display(Name = "Màu sắc")]
        [StringLength(50, ErrorMessage = "Màu sắc không được quá 50 ký tự")]
        public string Color { get; set; }

        [Required(ErrorMessage = "Kích thước sản phẩm là bắt buộc")]
        [Display(Name = "Kích thước")]
        [StringLength(50, ErrorMessage = "Kích thước không được quá 50 ký tự")]
        public string Size { get; set; }

        [Display(Name = "Số lượng")]
        public int Quantity { get; set; }
    }
}