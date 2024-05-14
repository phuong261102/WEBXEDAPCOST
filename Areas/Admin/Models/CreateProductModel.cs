using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using App.Models;

namespace XEDAPVIP.Areas.Admin.Models
{
    public class CreateProductModel : Product
    {
        [Display(Name = "Danh mục")]
        public int[]? CategoryId { get; set; }
        public List<ProductDetailEntry> ProductDetails { get; set; }
    }

    public class ProductDetailEntry
    {
        public string DetailsName { get; set; }
        public string DetailsValue { get; set; }
    }
}