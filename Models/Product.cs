using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using XEDAPVIP.Models;

namespace App.Models
{
    [Table("Product")]
    public class Product
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên sản phẩm là bắt buộc")]
        [StringLength(100, ErrorMessage = "Tên sản phẩm không được quá 100 ký tự")]
        [Display(Name = "Tên sản phẩm")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Mô tả sản phẩm là bắt buộc")]
        [Display(Name = "Mô tả sản phẩm")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Giá sản phẩm là bắt buộc")]
        [Display(Name = "Giá sản phẩm")]
        [DataType(DataType.Currency)]
        public double Price { get; set; }

        [Display(Name = "Giá giảm")]
        [DataType(DataType.Currency)]
        public double? DiscountPrice { get; set; } // Giá sau khi giảm

        [Display(Name = "Chuỗi định danh (url)", Prompt = "Nhập hoặc để trống tự phát sinh theo Title")]
        [StringLength(160, MinimumLength = 5, ErrorMessage = "{0} dài {1} đến {2}")]
        [RegularExpression(@"^[a-z0-9-]*$", ErrorMessage = "Chỉ dùng các ký tự [a-z0-9-]")]
        public string? Slug { set; get; }

        [Display(Name = "Ngày tạo")]
        public DateTime DateCreated { set; get; }

        [Display(Name = "Ngày cập nhật")]
        public DateTime DateUpdated { set; get; }

        [Display(Name = "Chi tiết sản phẩm")]
        public string DetailsJson { get; set; } // Thuộc tính để lưu trữ chi tiết sản phẩm dưới dạng chuỗi JSON

        [NotMapped]
        public Dictionary<string, string> DetailsDictionary
        {
            get => JsonConvert.DeserializeObject<Dictionary<string, string>>(DetailsJson ?? "{}");
            set => DetailsJson = JsonConvert.SerializeObject(value);
        }

        public List<ProductCategory> ProductCategories { get; set; }

        public List<ProductVariant> Variants { get; set; }

        [Display(Name = "Ảnh sản phẩm")]

        public string MainImage { get; set; }
        public IList<string>? SubImages { get; set; }
        public int BrandId { get; set; }
        public Brand Brand { get; set; }
    }
}
