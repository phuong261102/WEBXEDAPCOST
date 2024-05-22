using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using App.Models;

namespace XEDAPVIP.Models
{
    [Table("CartItems")]
    public class CartItem
    {
        public int Id { get; set; }

        public int CartId { get; set; }

        [ForeignKey("CartId")]
        public Cart Cart { get; set; }
        public int ProductId { get; set; }

        [ForeignKey("ProductId")]
        public Product Product { get; set; }
        public int productCode { get; set; } //for variants

        public int Quantity { get; set; }

        [DataType(DataType.Currency)]
        public double Price { get; set; }
    }
}