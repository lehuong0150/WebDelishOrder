using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using WebDelishOrder.Hubs;

namespace WebDelishOrder.Service
{
    public class OrderHubService
    {
        private readonly IHubContext<OrderHub> _hubContext;

        public OrderHubService(IHubContext<OrderHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task NotifyOrderStatusChange(int orderId, string status)
        {
            Console.WriteLine($"Gửi thông báo thay đổi trạng thái: orderId={orderId}, status={status}");
            await _hubContext.Clients.All.SendAsync("OrderStatusChanged", orderId, status);
        }

        public async Task NotifyNewOrder(int orderId)
        {
            Console.WriteLine($"Gửi thông báo đơn hàng mới: {orderId}");
            await _hubContext.Clients.All.SendAsync("ReceiveNewOrder", orderId);
        }
    }
}
