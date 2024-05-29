using System;
using App.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using XEDAPVIP.Areas.Home.Models.CheckOut;
using XEDAPVIP.ExtendMethods;

namespace XEDAPVIP.Services
{
    public interface IVnPayService
    {
        string CreatePaymentUrl(Order order, HttpContext context);
        bool ValidateResponse(IQueryCollection query);
    }

    public class VnPayService : IVnPayService
    {
        private readonly VnPaySettings _vnPaySettings;

        public VnPayService(IOptions<VnPaySettings> vnPaySettings)
        {
            _vnPaySettings = vnPaySettings.Value;
        }

        public string CreatePaymentUrl(Order order, HttpContext context)
        {
            var url = _vnPaySettings.Url;
            var returnUrl = _vnPaySettings.ReturnUrl;
            var tmnCode = _vnPaySettings.TmnCode;
            var hashSecret = _vnPaySettings.HashSecret;

            if (!decimal.TryParse(order.TotalAmount, out var totalAmount))
            {
                totalAmount = 0; // Fallback to 0 if conversion fails
            }

            var amountInCents = (long)(totalAmount * 100);
            var vnpay = new VnPayLibrary();
            vnpay.AddRequestData("vnp_Version", "2.1.0");
            vnpay.AddRequestData("vnp_Command", "pay");
            vnpay.AddRequestData("vnp_TmnCode", tmnCode);
            vnpay.AddRequestData("vnp_Amount", amountInCents.ToString());
            vnpay.AddRequestData("vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss"));
            vnpay.AddRequestData("vnp_CurrCode", "VND");
            vnpay.AddRequestData("vnp_IpAddr", context.Connection.RemoteIpAddress.ToString());
            vnpay.AddRequestData("vnp_Locale", "vn");
            vnpay.AddRequestData("vnp_OrderInfo", $"Thanh toan don hang {order.Id}");
            vnpay.AddRequestData("vnp_OrderType", "other");
            vnpay.AddRequestData("vnp_ReturnUrl", returnUrl);
            vnpay.AddRequestData("vnp_TxnRef", order.Id.ToString());

            var paymentUrl = vnpay.CreateRequestUrl(url, hashSecret);
            return paymentUrl;
        }

        public bool ValidateResponse(IQueryCollection query)
        {
            var vnpay = new VnPayLibrary();
            foreach (var (key, value) in query)
            {
                vnpay.AddResponseData(key, value);
            }
            long orderId = Convert.ToInt64(vnpay.GetResponseData("vnp_TxnRef"));
            long vnpayTranId = Convert.ToInt64(vnpay.GetResponseData("vnp_TransactionNo"));
            string vnp_ResponseCode = vnpay.GetResponseData("vnp_ResponseCode");
            string vnp_TransactionStatus = vnpay.GetResponseData("vnp_TransactionStatus");
            long vnp_Amount = Convert.ToInt64(vnpay.GetResponseData("vnp_Amount")) / 100;
            var vnp_SecureHash = query["vnp_SecureHash"];
            var hashSecret = _vnPaySettings.HashSecret;

            bool checkSignature = vnpay.ValidateSignature(vnp_SecureHash, hashSecret);


            return checkSignature;
        }
    }
}
