using Microsoft.AspNetCore.SignalR;

namespace WebDelishOrder.Hubs
{
    public class OrderHub : Hub
    {
        // Gửi thông báo đến tất cả client về đơn hàng mới
        public async Task NotifyNewOrder(int orderId)
        {
            Console.WriteLine($"Gửi thông báo đơn hàng mới: {orderId}");
            await Clients.All.SendAsync("ReceiveNewOrder", orderId);
        }

        // Gửi thông báo thay đổi trạng thái đơn hàng
        public async Task NotifyOrderStatusChange(int orderId, string status)
        {
            Console.WriteLine($"Gửi thông báo thay đổi trạng thái: orderId={orderId}, status={status}");
            // Gửi thông báo đến tất cả các client
            await Clients.All.SendAsync("OrderStatusChanged", orderId, status);
        }

        // Phương thức được gọi từ client để lấy trạng thái đơn hàng
        public async Task GetOrderStatus(int orderId)
        {
            Console.WriteLine($"Nhận yêu cầu lấy trạng thái đơn hàng: {orderId}");
            // Xử lý logic lấy trạng thái đơn hàng
            string status = GetStatusFromDatabase(orderId);

            Console.WriteLine($"Gửi trạng thái đơn hàng về client: orderId={orderId}, status={status}");
            // Gửi trạng thái về cho client gọi hàm
            await Clients.Caller.SendAsync("OrderConfirmed", orderId, status);
        }

        // Phương thức được gọi từ hệ thống admin khi xác nhận đơn hàng
        public async Task ConfirmOrder(int orderId, string status)
        {
            Console.WriteLine($"Xác nhận đơn hàng: orderId={orderId}, status={status}");
            // Xử lý logic xác nhận đơn hàng

            Console.WriteLine($"Gửi thông báo xác nhận đơn hàng: orderId={orderId}, status={status}");
            // Gửi thông báo đến tất cả client đang kết nối
            await Clients.All.SendAsync("OrderConfirmed", orderId, status);
        }

        // Phương thức để kiểm tra kết nối
        public string Echo(string message)
        {
            string connectionId = Context.ConnectionId;
            Console.WriteLine($"Nhận Echo từ client: {message} (Connection ID: {connectionId})");
            return "Server received: " + message;
        }

        private string GetStatusFromDatabase(int orderId)
        {
            // Logic lấy trạng thái từ database
            // Đây là phiên bản mô phỏng
            return "Confirmed";
        }
    }
}
