using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace App.Models
{
    [Table("Orders")]
    public class Order
    {
        [Key]
        public int Id { get; set; }

        // UserId is now nullable to allow guest orders
        public string? UserId { get; set; }

        [StringLength(100)]
        public string UserName { get; set; }

        [EmailAddress]
        [StringLength(100)]
        public string UserEmail { get; set; }

        [StringLength(100)]
        public string PhoneNumber { get; set; }

        [StringLength(100)]
        public string? OrderNote { get; set; }

        [Required]
        public DateTime OrderDate { get; set; }

        public DateTime? ShippedDate { get; set; }

        [Required]
        [DataType(DataType.Currency)]
        public string TotalAmount { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; } // E.g., "Pending", "Shipped", "Delivered", etc.

        public string? ShippingAddress { get; set; }

        [Required]
        [StringLength(50)]
        public string ShippingMethod { get; set; } // E.g., "Standard", "Express"

        [Required]
        [StringLength(50)]
        public string PaymentMethod { get; set; } // E.g., "Credit Card", "PayPal"

        // Navigation property
        public List<OrderDetail> OrderDetails { get; set; }
    }
}
