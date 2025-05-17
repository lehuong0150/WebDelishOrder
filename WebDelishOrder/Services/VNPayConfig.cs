namespace WebDelishOrder.Services
{
    public static class VNPayConfig
    {
        public static readonly string vnp_TmnCode = "E7RVJIIW";
        public static readonly string vnp_HashSecret = "82MGW0UDAAD3ADYQ4S2Z4ZHT3AT6N6QW";
        public static readonly string vnp_Url = "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";
        public static readonly string vnp_ReturnUrl = "appdelishorder://vnpay_return"; // Deep link về app
        public static readonly string frontendUrl = "appdelishorder://vnpay_return";
    }
}