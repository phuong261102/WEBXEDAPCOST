using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using App.Models;

namespace XEDAPVIP.Models
{
    [Table("Carts")]
    public class Cart
    {
        public int Id { get; set; }

        public string? UserId { get; set; } // Nullable for guest users
        public AppUser User { get; set; }

        [StringLength(100)]
        public string? SessionId { get; set; } // Session identifier for guest users

        [Display(Name = "Ngày tạo")]
        public DateTime DateCreated { get; set; }

        [Display(Name = "Ngày cập nhật")]
        public DateTime DateUpdated { get; set; }

        public List<CartItem> CartItems { get; set; } = new List<CartItem>();
    }
}