using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace App.Models
{
    [Table("ProductCategory")]
    public class ProductCategory
    {
        public int ProductId { get; set; }
        [JsonIgnore]
        public Product Product { get; set; }

        public int CategoryId { get; set; }
        public Category Category { get; set; }
    }
}