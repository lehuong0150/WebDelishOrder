using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using WebDelishOrder.Services;

namespace WebDelishOrder.APIControllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VNPayApiController : ControllerBase
    {
        [HttpGet("create")]
        public IActionResult CreatePayment([FromQuery] string amount, [FromQuery] string orderId)
        {
            var vnp_Params = new Dictionary<string, string>
            {
                { "vnp_Version", "2.1.0" },
                { "vnp_Command", "pay" },
                { "vnp_TmnCode", VNPayConfig.vnp_TmnCode },
                { "vnp_Amount", (long.Parse(amount) * 100).ToString() },
                { "vnp_CurrCode", "VND" },
                { "vnp_TxnRef", orderId },
                { "vnp_OrderInfo", "Thanh toán đơn hàng: " + orderId },
                { "vnp_OrderType", "other" },
                { "vnp_Locale", "vn" },
                { "vnp_ReturnUrl", VNPayConfig.vnp_ReturnUrl },
                { "vnp_IpAddr", HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1" },
                { "vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss") }
            };

            // Sắp xếp tham số theo thứ tự alphabet
            var fieldNames = vnp_Params.Keys.OrderBy(x => x).ToList();

            var hashData = new StringBuilder();
            var query = new StringBuilder();
            for (int i = 0; i < fieldNames.Count; i++)
            {
                var name = fieldNames[i];
                var value = vnp_Params[name];
                if (!string.IsNullOrEmpty(value))
                {
                    hashData.Append(name + "=" + WebUtility.UrlEncode(value));
                    query.Append(name + "=" + WebUtility.UrlEncode(value));
                    if (i < fieldNames.Count - 1)
                    {
                        hashData.Append("&");
                        query.Append("&");
                    }
                }
            }

            // Tạo chuỗi hash với HMAC SHA512
            string secureHash = HmacSHA512(VNPayConfig.vnp_HashSecret, hashData.ToString());
            query.Append("&vnp_SecureHash=").Append(secureHash);

            string paymentUrl = VNPayConfig.vnp_Url + "?" + query.ToString();

            return Ok(new { paymentUrl });
        }

        [HttpGet("vnpay-return")]
        public IActionResult HandleVnpayReturn()
        {
            // Lấy tất cả tham số từ query
            var vnpParams = new Dictionary<string, string>();
            foreach (var key in Request.Query.Keys)
            {
                vnpParams[key] = Request.Query[key];
            }

            // Lấy và loại bỏ vnp_SecureHash khỏi tham số để xác thực
            vnpParams.TryGetValue("vnp_SecureHash", out string vnpSecureHash);
            vnpParams.Remove("vnp_SecureHash");

            // Sắp xếp và tạo chuỗi hash
            var fieldNames = vnpParams.Keys.OrderBy(x => x).ToList();
            var hashData = new StringBuilder();
            for (int i = 0; i < fieldNames.Count; i++)
            {
                var name = fieldNames[i];
                var value = vnpParams[name];
                hashData.Append(name + "=" + WebUtility.UrlEncode(value));
                if (i < fieldNames.Count - 1)
                    hashData.Append("&");
            }

            string secureHash = HmacSHA512(VNPayConfig.vnp_HashSecret, hashData.ToString());

            // Deep link app Android
            string frontendUrl = "appdelishorder://vnpay_return";
            string redirectUrl;

            if (secureHash.Equals(vnpSecureHash, StringComparison.InvariantCultureIgnoreCase))
            {
                string responseCode = vnpParams.ContainsKey("vnp_ResponseCode") ? vnpParams["vnp_ResponseCode"] : "";
                string txnRef = vnpParams.ContainsKey("vnp_TxnRef") ? vnpParams["vnp_TxnRef"] : "";

                // Redirect về app với TransactionStatus và orderId
                redirectUrl = $"{frontendUrl}?TransactionStatus={responseCode}&orderId={txnRef}";
            }
            else
            {
                redirectUrl = $"{frontendUrl}?TransactionStatus=error";
            }
            // Thêm dòng này để log ra console
            Console.WriteLine("VNPay redirect URL: " + redirectUrl);

            return Redirect(redirectUrl);
        }

        // Hàm tạo HMAC SHA512
        public static string HmacSHA512(string key, string data)
        {
            using (var hmac = new System.Security.Cryptography.HMACSHA512(Encoding.UTF8.GetBytes(key)))
            {
                var hashValue = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
                var hex = BitConverter.ToString(hashValue).Replace("-", "").ToLower();
                return hex;
            }
        }
    }
}