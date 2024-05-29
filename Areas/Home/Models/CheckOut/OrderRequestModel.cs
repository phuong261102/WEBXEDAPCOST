using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace XEDAPVIP.Areas.Home.Models.CheckOut
{
    public class OrderRequestModel
    {
        public string? UserId { get; set; }
        public string FullName { get; set; }
        public string PhoneNumber { get; set; }
        public string EmailAddress { get; set; }
        public string OrderNote { get; set; }
        public string ShippingMethod { get; set; }
        public string PaymentMethod { get; set; }
        public int TotalAmount { get; set; }
        public string Status { get; set; }
        public string ShippingAddress { get; set; }
        public List<int>? CartItemIds { get; set; }
    }
}