using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using XEDAPVIP.Models;

namespace XEDAPVIP.Areas.Home.Models.CheckOut
{
    public class ProfileCheckoutModel
    {
        public string? UserId { get; set; }

        [Display(Name = "Tên tài khoản")]
        [Required(ErrorMessage = "Tên đặt hàng là bắt buộc.")]
        public string UserName { get; set; }

        [Display(Name = "Địa chỉ email")]
        [Required(ErrorMessage = "Địa chỉ email là bắt buộc.")]
        [EmailAddress(ErrorMessage = "Địa chỉ email không hợp lệ.")]
        public string UserEmail { get; set; }

        [Display(Name = "Số điện thoại")]
        [Required(ErrorMessage = "Số điện thoại là bắt buộc.")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
        public string PhoneNumber { get; set; }

        [Display(Name = "Địa chỉ mặc định")]
        [StringLength(400, ErrorMessage = "Địa chỉ không được vượt quá 400 ký tự.")]
        public string HomeAddress { get; set; }

        [Display(Name = "Số nhà")]
        [Required(ErrorMessage = "Số nhà là bắt buộc.")]
        public string StreetNumber { get; set; }

        [Display(Name = "Tỉnh/Thành phố")]
        [Required(ErrorMessage = "Vui lòng chọn Tỉnh/Thành phố.")]
        public string SelectedProvince { get; set; }

        [Display(Name = "Quận/Huyện")]
        [Required(ErrorMessage = "Vui lòng chọn Quận/Huyện.")]
        public string SelectedDistrict { get; set; }

        [Display(Name = "Phường/Xã")]
        [Required(ErrorMessage = "Vui lòng chọn Phường/Xã.")]
        public string SelectedWard { get; set; }

        public IEnumerable<SelectListItem> ProvinceOptions { get; set; }
        public IEnumerable<SelectListItem> DistrictOptions { get; set; }
        public IEnumerable<SelectListItem> WardOptions { get; set; }

        public List<AddressModel> Addresses { get; set; }

        public List<CartItem> cartItems { get; set; }
        public class Province
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public List<District> Districts { get; set; }
        }

        public class District
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public List<Ward> Wards { get; set; }
        }

        public class Ward
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public string Level { get; set; }
        }

        public class AddressModel
        {
            public string StreetNumber { get; set; }
            public string SelectedProvince { get; set; }
            public string SelectedDistrict { get; set; }
            public string SelectedWard { get; set; }
            public bool IsDefault { get; set; }
        }
    }
}
