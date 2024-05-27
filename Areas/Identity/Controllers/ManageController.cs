// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Threading.Tasks;
using App.Areas.Identity.Models.ManageViewModels;
using App.ExtendMethods;
using App.Models;
using App.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace App.Areas.Identity.Controllers
{

    [Area("Identity")]
    [Route("/Member/[action]")]
    public class ManageController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<ManageController> _logger;
        private readonly HttpClient _httpClient;
        private readonly AppDbContext _context;

        public ManageController(
        UserManager<AppUser> userManager,
        SignInManager<AppUser> signInManager,
        IEmailSender emailSender,
        ILogger<ManageController> logger,
        AppDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailSender = emailSender;
            _logger = logger;
            _httpClient = new HttpClient();
            _context = context;
        }

        //
        // GET: /Manage/Index
        [HttpGet]
        public async Task<IActionResult> Index(ManageMessageId? message = null)
        {
            ViewData["SuccessMessage"] =
                message == ManageMessageId.ChangePasswordSuccess ? "Đã thay đổi mật khẩu."
                : message == ManageMessageId.SetPasswordSuccess ? "Đã đặt lại mật khẩu."
                : message == ManageMessageId.SetTwoFactorSuccess ? "Your two-factor authentication provider has been set."
                : message == ManageMessageId.Error ? "Có lỗi."
                : message == ManageMessageId.AddPhoneSuccess ? "Đã thêm số điện thoại."
                : message == ManageMessageId.RemovePhoneSuccess ? "Đã bỏ số điện thoại."
                : "";

            var user = await GetCurrentUserAsync();
            var model = new IndexViewModel
            {
                HasPassword = await _userManager.HasPasswordAsync(user),
                PhoneNumber = await _userManager.GetPhoneNumberAsync(user),
                TwoFactor = await _userManager.GetTwoFactorEnabledAsync(user),
                Logins = await _userManager.GetLoginsAsync(user),
                BrowserRemembered = await _signInManager.IsTwoFactorClientRememberedAsync(user),
                AuthenticatorKey = await _userManager.GetAuthenticatorKeyAsync(user),
                profile = new EditExtraProfileModel()
                {
                    BirthDate = user.BirthDate,
                    HomeAddress = user.HomeAddress,
                    UserName = user.UserName,
                    UserEmail = user.Email,
                    PhoneNumber = user.PhoneNumber,
                }
            };
            return View(model);
        }
        public enum ManageMessageId
        {
            AddPhoneSuccess,
            AddLoginSuccess,
            ChangePasswordSuccess,
            SetTwoFactorSuccess,
            SetPasswordSuccess,
            RemoveLoginSuccess,
            RemovePhoneSuccess,
            Error
        }
        private Task<AppUser> GetCurrentUserAsync()
        {
            return _userManager.GetUserAsync(HttpContext.User);
        }

        //
        // GET: /Manage/ChangePassword
        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View();
        }

        //
        // POST: /Manage/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var user = await GetCurrentUserAsync();
            if (user != null)
            {
                var result = await _userManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);
                if (result.Succeeded)
                {
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    _logger.LogInformation(3, "User changed their password successfully.");
                    return RedirectToAction(nameof(Index), new { Message = ManageMessageId.ChangePasswordSuccess });
                }
                ModelState.AddModelError(result);
                return View(model);
            }
            return RedirectToAction(nameof(Index), new { Message = ManageMessageId.Error });
        }
        //
        // GET: /Manage/SetPassword
        [HttpGet]
        public IActionResult SetPassword()
        {
            return View();
        }

        //
        // POST: /Manage/SetPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetPassword(SetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await GetCurrentUserAsync();
            if (user != null)
            {
                var result = await _userManager.AddPasswordAsync(user, model.NewPassword);
                if (result.Succeeded)
                {
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return RedirectToAction(nameof(Index), new { Message = ManageMessageId.SetPasswordSuccess });
                }
                ModelState.AddModelError(result);
                return View(model);
            }
            return RedirectToAction(nameof(Index), new { Message = ManageMessageId.Error });
        }

        //GET: /Manage/ManageLogins
        [HttpGet]
        public async Task<IActionResult> ManageLogins(ManageMessageId? message = null)
        {
            ViewData["StatusMessage"] =
                message == ManageMessageId.RemoveLoginSuccess ? "Đã loại bỏ liên kết tài khoản."
                : message == ManageMessageId.AddLoginSuccess ? "Đã thêm liên kết tài khoản"
                : message == ManageMessageId.Error ? "Có lỗi."
                : "";
            var user = await GetCurrentUserAsync();
            if (user == null)
            {
                return View("Error");
            }
            var userLogins = await _userManager.GetLoginsAsync(user);
            var schemes = await _signInManager.GetExternalAuthenticationSchemesAsync();
            var otherLogins = schemes.Where(auth => userLogins.All(ul => auth.Name != ul.LoginProvider)).ToList();
            ViewData["ShowRemoveButton"] = user.PasswordHash != null || userLogins.Count > 1;
            return View(new ManageLoginsViewModel
            {
                CurrentLogins = userLogins,
                OtherLogins = otherLogins
            });
        }


        //
        // POST: /Manage/LinkLogin
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult LinkLogin(string provider)
        {
            // Request a redirect to the external login provider to link a login for the current user
            var redirectUrl = Url.Action("LinkLoginCallback", "Manage");
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl, _userManager.GetUserId(User));
            return Challenge(properties, provider);
        }

        //
        // GET: /Manage/LinkLoginCallback
        [HttpGet]
        public async Task<ActionResult> LinkLoginCallback()
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
            {
                return View("Error");
            }
            var info = await _signInManager.GetExternalLoginInfoAsync(await _userManager.GetUserIdAsync(user));
            if (info == null)
            {
                return RedirectToAction(nameof(ManageLogins), new { Message = ManageMessageId.Error });
            }
            var result = await _userManager.AddLoginAsync(user, info);
            var message = result.Succeeded ? ManageMessageId.AddLoginSuccess : ManageMessageId.Error;
            return RedirectToAction(nameof(ManageLogins), new { Message = message });
        }


        //
        // POST: /Manage/RemoveLogin
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveLogin(RemoveLoginViewModel account)
        {
            ManageMessageId? message = ManageMessageId.Error;
            var user = await GetCurrentUserAsync();
            if (user != null)
            {
                var result = await _userManager.RemoveLoginAsync(user, account.LoginProvider, account.ProviderKey);
                if (result.Succeeded)
                {
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    message = ManageMessageId.RemoveLoginSuccess;
                }
            }
            return RedirectToAction(nameof(ManageLogins), new { Message = message });
        }
        //
        // GET: /Manage/AddPhoneNumber
        public IActionResult AddPhoneNumber()
        {
            return View();
        }

        //
        // POST: /Manage/AddPhoneNumber
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddPhoneNumber(AddPhoneNumberViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            // Generate the token and send it
            var user = await GetCurrentUserAsync();
            var code = await _userManager.GenerateChangePhoneNumberTokenAsync(user, model.PhoneNumber);
            await _emailSender.SendSmsAsync(model.PhoneNumber, "Mã xác thực là: " + code);
            return RedirectToAction(nameof(VerifyPhoneNumber), new { PhoneNumber = model.PhoneNumber });
        }
        //
        // GET: /Manage/VerifyPhoneNumber
        [HttpGet]
        public async Task<IActionResult> VerifyPhoneNumber(string phoneNumber)
        {
            var code = await _userManager.GenerateChangePhoneNumberTokenAsync(await GetCurrentUserAsync(), phoneNumber);
            // Send an SMS to verify the phone number
            return phoneNumber == null ? View("Error") : View(new VerifyPhoneNumberViewModel { PhoneNumber = phoneNumber });
        }

        //
        // POST: /Manage/VerifyPhoneNumber
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyPhoneNumber(VerifyPhoneNumberViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var user = await GetCurrentUserAsync();
            if (user != null)
            {
                var result = await _userManager.ChangePhoneNumberAsync(user, model.PhoneNumber, model.Code);
                if (result.Succeeded)
                {
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return RedirectToAction(nameof(Index), new { Message = ManageMessageId.AddPhoneSuccess });
                }
            }
            // If we got this far, something failed, redisplay the form
            ModelState.AddModelError(string.Empty, "Lỗi thêm số điện thoại");
            return View(model);
        }
        //
        // GET: /Manage/RemovePhoneNumber
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemovePhoneNumber()
        {
            var user = await GetCurrentUserAsync();
            if (user != null)
            {
                var result = await _userManager.SetPhoneNumberAsync(user, null);
                if (result.Succeeded)
                {
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return RedirectToAction(nameof(Index), new { Message = ManageMessageId.RemovePhoneSuccess });
                }
            }
            return RedirectToAction(nameof(Index), new { Message = ManageMessageId.Error });
        }


        //
        // POST: /Manage/EnableTwoFactorAuthentication
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EnableTwoFactorAuthentication()
        {
            var user = await GetCurrentUserAsync();
            if (user != null)
            {
                await _userManager.SetTwoFactorEnabledAsync(user, true);
                await _signInManager.SignInAsync(user, isPersistent: false);
            }
            return RedirectToAction(nameof(Index), "Manage");
        }

        //
        // POST: /Manage/DisableTwoFactorAuthentication
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DisableTwoFactorAuthentication()
        {
            var user = await GetCurrentUserAsync();
            if (user != null)
            {
                await _userManager.SetTwoFactorEnabledAsync(user, false);
                await _signInManager.SignInAsync(user, isPersistent: false);
                _logger.LogInformation(2, "User disabled two-factor authentication.");
            }
            return RedirectToAction(nameof(Index), "Manage");
        }
        //
        // POST: /Manage/ResetAuthenticatorKey
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetAuthenticatorKey()
        {
            var user = await GetCurrentUserAsync();
            if (user != null)
            {
                await _userManager.ResetAuthenticatorKeyAsync(user);
                _logger.LogInformation(1, "User reset authenticator key.");
            }
            return RedirectToAction(nameof(Index), "Manage");
        }

        //
        // POST: /Manage/GenerateRecoveryCode
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateRecoveryCode()
        {
            var user = await GetCurrentUserAsync();
            if (user != null)
            {
                var codes = await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 5);
                _logger.LogInformation(1, "User generated new recovery code.");
                return View("DisplayRecoveryCodes", new DisplayRecoveryCodesViewModel { Codes = codes });
            }
            return View("Error");
        }

        [HttpGet]
        public async Task<IActionResult> GetProvinceDistrictWard()
        {
            try
            {
                var apiUrl = "https://raw.githubusercontent.com/kenzouno1/DiaGioiHanhChinhVN/master/data.json";
                HttpResponseMessage response = await _httpClient.GetAsync(apiUrl);

                if (response.IsSuccessStatusCode)
                {
                    var apiData = await response.Content.ReadAsStringAsync();
                    return Ok(apiData);
                }
                else
                {
                    // Handle other status codes
                    return StatusCode((int)response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
                return StatusCode(500);
            }
        }

        public async Task<List<Province>> GetProvincesAsync()
        {
            var dataDiagioiResponse = await GetProvinceDistrictWard();

            if (dataDiagioiResponse is OkObjectResult okResult && okResult.Value != null)
            {
                var apiData = okResult.Value.ToString();
                var dataProvinces = JsonConvert.DeserializeObject<List<Province>>(apiData);
                return dataProvinces;
            }
            else
            {
                // Handle the case where the response is not successful
                return new List<Province>();
            }
        }

        public async Task<JsonResult> GetDistrictsByProvinceId(string provinceId)
        {
            var provinces = await GetProvincesAsync();

            var selectedProvince = provinces.FirstOrDefault(province => province.Id == provinceId);

            if (selectedProvince != null)
            {
                return Json(selectedProvince.Districts);
            }
            else
            {
                return Json(new List<District>());
            }
        }

        public async Task<JsonResult> GetWardsByDistrictId(string districtId)
        {
            var provinces = await GetProvincesAsync();

            District selectedDistrict = null;
            foreach (var province in provinces)
            {
                selectedDistrict = province.Districts.FirstOrDefault(district => district.Id == districtId);
                if (selectedDistrict != null)
                {
                    break;
                }
            }

            if (selectedDistrict != null)
            {
                return Json(selectedDistrict.Wards);
            }
            else
            {
                return Json(new List<Ward>());
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddAddress(AddressModel model)
        {
            if (ModelState.IsValid)
            {
                var address = new Address
                {
                    StreetNumber = model.StreetNumber,
                    SelectedWard = model.SelectedWard,
                    SelectedDistrict = model.SelectedDistrict,
                    SelectedProvince = model.SelectedProvince,
                };

                var user = await _userManager.GetUserAsync(HttpContext.User);
                address.UserId = user.Id;

                _context.Addresses.Add(address);

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Thêm địa chỉ thành công";

                return Json(new { success = true, newAddressFormatted = $"{address.StreetNumber}, {address.SelectedWard}, {address.SelectedDistrict}, {address.SelectedProvince}" });
            }
            else
            {
                return Json(new { success = false, errorMessage = "Thông tin địa chỉ không hợp lệ." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteAddress(int index)
        {
            var user = await _userManager.GetUserAsync(HttpContext.User);
            var addresses = await _context.Addresses.Where(a => a.UserId == user.Id).ToListAsync();

            if (index >= 0 && index < addresses.Count)
            {
                var addressToDelete = addresses[index];
                _context.Addresses.Remove(addressToDelete);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Xoá địa chỉ thành công";

                return Json(new { success = true });
            }
            return Json(new { success = false });
        }
        [HttpGet]
        public async Task<IActionResult> GetAddresses()
        {
            var user = await _userManager.GetUserAsync(HttpContext.User);

            if (user == null)
            {
                return NotFound(); // Trả về lỗi nếu không tìm thấy người dùng
            }

            // Query the database to get the list of addresses
            var addresses = await _context.Addresses.ToListAsync();

            // Map the list of addresses to a new list containing only the address names
            var addressNames = new List<string>();
            foreach (var address in addresses)
            {
                var selectedProvince = await GetProvinceNameById(address.SelectedProvince);
                var selectedDistrict = await GetDistrictNameById(address.SelectedDistrict);
                var selectedWard = await GetWardNameById(address.SelectedWard);

                var addressName = $"{address.StreetNumber}, {selectedWard}, {selectedDistrict}, {selectedProvince}";
                addressNames.Add(addressName);
            }

            return Json(addressNames); // Return the list of address names as JSON
        }





        [HttpPost]
        public async Task<IActionResult> SetDefaultAddress(int index)
        {
            var user = await _userManager.GetUserAsync(HttpContext.User);
            var addresses = await _context.Addresses.Where(a => a.UserId == user.Id).ToListAsync();

            if (index >= 0 && index < addresses.Count)
            {
                var defaultAddress = addresses[index];
                var provinceName = await GetProvinceNameById(defaultAddress.SelectedProvince);
                var districtName = await GetDistrictNameById(defaultAddress.SelectedDistrict);
                var wardName = await GetWardNameById(defaultAddress.SelectedWard);
                user.HomeAddress = $"{defaultAddress.StreetNumber}, {wardName}, {districtName}, {provinceName}";
                await _userManager.UpdateAsync(user);
                await _signInManager.RefreshSignInAsync(user);

                return Ok(new { success = true });
            }
            return Ok(new { success = false });
        }

        private async Task<string> GetProvinceNameById(string provinceId)
        {
            var provinces = await GetProvincesAsync();
            var province = provinces.FirstOrDefault(p => p.Id == provinceId);
            return province != null ? province.Name : "";
        }

        private async Task<string> GetDistrictNameById(string districtId)
        {
            var provinces = await GetProvincesAsync();
            foreach (var province in provinces)
            {
                var district = province.Districts.FirstOrDefault(d => d.Id == districtId);
                if (district != null)
                {
                    return district.Name;
                }
            }
            return "";
        }

        private async Task<string> GetWardNameById(string wardId)
        {
            var provinces = await GetProvincesAsync();
            foreach (var province in provinces)
            {
                foreach (var district in province.Districts)
                {
                    var ward = district.Wards.FirstOrDefault(w => w.Id == wardId);
                    if (ward != null)
                    {
                        return ward.Name;
                    }
                }
            }
            return "";
        }

        public async Task<IActionResult> EditProfileAsync()
        {
            var user = await GetCurrentUserAsync();
            var provinces = await GetProvincesAsync();

            var provinceOptions = provinces.Select(p => new SelectListItem
            {
                Value = p.Id.ToString(),
                Text = p.Name
            }).ToList();

            var addresses = await _context.Addresses.Where(a => a.UserId == user.Id).ToListAsync();

            var addressModels = new List<AddressModel>();
            foreach (var address in addresses)
            {
                var provinceName = await GetProvinceNameById(address.SelectedProvince);
                var districtName = await GetDistrictNameById(address.SelectedDistrict);
                var wardName = await GetWardNameById(address.SelectedWard);

                var addressModel = new AddressModel
                {
                    StreetNumber = address.StreetNumber,
                    SelectedProvince = provinceName,
                    SelectedDistrict = districtName,
                    SelectedWard = wardName
                };
                addressModels.Add(addressModel);
            }

            var model = new EditExtraProfileModel()
            {
                UserName = user.UserName,
                UserEmail = user.Email,
                PhoneNumber = user.PhoneNumber,
                HomeAddress = user.HomeAddress,
                BirthDate = user.BirthDate,
                ProvinceOptions = provinceOptions,
                DistrictOptions = new List<SelectListItem>(),
                WardOptions = new List<SelectListItem>(),
                Addresses = addressModels
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> EditProfile(EditExtraProfileModel model)
        {
            var user = await GetCurrentUserAsync();

            user.BirthDate = model.BirthDate;
            user.PhoneNumber = model.PhoneNumber;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View(model);
            }
            TempData["SuccessMessage"] = "Lưu thông tin thành công";
            await _signInManager.RefreshSignInAsync(user);
            return RedirectToAction(nameof(Index));
        }
        public async Task<IActionResult> RemoveHomeAddress()
        {
            var user = await _userManager.GetUserAsync(HttpContext.User);
            if (user != null && user.HomeAddress != null)
            {
                user.HomeAddress = null;
                var result = await _userManager.UpdateAsync(user);
                if (result.Succeeded)
                {
                    TempData["SuccessMessage"] = "Xoá địa chỉ mặc định thành công";
                }
                else
                {
                    TempData["ErrorMessage"] = "Có lỗi xảy ra khi xoá địa chỉ mặc định";
                }
            }
            return RedirectToAction(nameof(Index));
        }

    }
}