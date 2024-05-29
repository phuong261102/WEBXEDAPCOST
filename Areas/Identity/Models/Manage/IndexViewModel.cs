// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using App.Models;
using Microsoft.AspNetCore.Identity;

namespace App.Areas.Identity.Models.ManageViewModels
{
    public class IndexViewModel
    {
        public EditExtraProfileModel profile { get; set; }

        public string FormattedBirthDate
        {
            get
            {
                return profile.BirthDate?.ToString("dd/MM/yyyy");
            }
        }
        public bool HasPassword { get; set; }

        public IList<UserLoginInfo> Logins { get; set; }

        public string PhoneNumber { get; set; }

        public bool TwoFactor { get; set; }

        public bool BrowserRemembered { get; set; }

        public string AuthenticatorKey { get; set; }
        // Thêm thuộc tính mới
        public List<OrderViewModel> Orders { get; set; }
    }
    public class OrderViewModel
    {
        public int OrderId { get; set; }
        public string UserName { get; set; }
        public string UserEmail { get; set; }
        public string PhoneNumber { get; set; }
        public string OrderNote { get; set; }
        public DateTime OrderDate { get; set; }
        public DateTime? ShippedDate { get; set; }
        public int TotalAmount { get; set; }
        public string Status { get; set; }
        public string ShippingAddress { get; set; }
        public string ShippingMethod { get; set; }
        public string PaymentMethod { get; set; }
        public List<OrderDetailViewModel> OrderDetails { get; set; }
    }

    public class OrderDetailViewModel
    {
        public int? OrderId { get; set; }
        public string ProductName { get; set; }
        public string ProductDescription { get; set; }
        public string ProductImage { get; set; }
        public int? VariantId { get; set; }
        public ProductVariant? Variant { get; set; }
        public int Quantity { get; set; }
        public double UnitPrice { get; set; }
        public double TotalPrice { get; set; }
    }
}
