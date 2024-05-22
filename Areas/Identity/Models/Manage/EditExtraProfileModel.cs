using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace App.Areas.Identity.Models.ManageViewModels
{
  public class EditExtraProfileModel
  {
    [Display(Name = "Tên tài khoản")]
    public string UserName { get; set; }

    [Display(Name = "Địa chỉ email")]
    public string UserEmail { get; set; }

    [Display(Name = "Số điện thoại")]
    public string PhoneNumber { get; set; }

    [Display(Name = "Địa chỉ mặc định")]
    [StringLength(400)]
    public string HomeAddress { get; set; }

    [Display(Name = "Ngày sinh")]
    [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}", ApplyFormatInEditMode = true)]
    public DateTime? BirthDate { get; set; }

    [Display(Name = "Số nhà")]
    public string StreetNumber { get; set; }

    [Display(Name = "Tỉnh/Thành phố")]
    public string SelectedProvince { get; set; }

    [Display(Name = "Quận/Huyện")]
    public string SelectedDistrict { get; set; }

    [Display(Name = "Phường/Xã")]
    public string SelectedWard { get; set; }

    public IEnumerable<SelectListItem> ProvinceOptions { get; set; }
    public IEnumerable<SelectListItem> DistrictOptions { get; set; }
    public IEnumerable<SelectListItem> WardOptions { get; set; }

    public List<AddressModel> Addresses { get; set; }

    public void AddAddress(AddressModel address)
    {
      if (Addresses == null)
      {
        Addresses = new List<AddressModel>();
      }
      Addresses.Add(address);
    }

    public void RemoveAddress(int index)
    {
      if (Addresses != null && Addresses.Count > index)
      {
        Addresses.RemoveAt(index);
      }
    }

    public void UpdateAddress(int index, AddressModel address)
    {
      if (Addresses != null && Addresses.Count > index)
      {
        Addresses[index] = address;
      }
    }


  }

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
