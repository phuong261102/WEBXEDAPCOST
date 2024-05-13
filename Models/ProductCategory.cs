using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace App.Models
{
    [Table("ProductCategory")]
    public class ProductCategory
    {
        public int ProductID { set; get; }

        public int CategoryID { set; get; }

        [ForeignKey("ProductID")]
        public Product Product { set; get; }

        [ForeignKey("CategoryID")]
        public Category Category { set; get; }
    }
}