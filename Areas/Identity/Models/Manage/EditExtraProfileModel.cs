using System;
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

    [Display(Name = "Địa chỉ")]
    [StringLength(400)]
    public string HomeAddress { get; set; }

    [Display(Name = "Ngày sinh")]
    public DateTime? BirthDate { get; set; }

    // [Display(Name = "Số nhà")]
    // public string StreetNumber { get; set; }

    [Display(Name = "Tỉnh/Thành phố")]
    public string SelectedProvince { get; set; }
    [Display(Name = "Quận/Huyện")]
    public string SelectedDistrict { get; set; }
    [Display(Name = "Phường/Xã")]
    public string SelectedWard { get; set; }

    // Thêm thuộc tính để lưu trữ danh sách lựa chọn
    public IEnumerable<SelectListItem> ProvinceOptions { get; set; }
    public IEnumerable<SelectListItem> DistrictOptions { get; set; }
    public IEnumerable<SelectListItem> WardOptions { get; set; }
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