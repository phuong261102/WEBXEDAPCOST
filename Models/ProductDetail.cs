using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace App.Models
{
    [Table("ProductDetail")]
    public class ProductDetail
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "ProductId là bắt buộc")]
        public int ProductId { get; set; }

        [Required(ErrorMessage = "Tên thông số là bắt buộc")]
        [Display(Name = "Tên thông số")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Giá trị thông số là bắt buộc")]
        [Display(Name = "Giá trị thông số")]
        public string Value { get; set; }

        // Navigation property để tham chiếu tới sản phẩm mà thông số này thuộc về
        public virtual Product Product { get; set; }
    }
}