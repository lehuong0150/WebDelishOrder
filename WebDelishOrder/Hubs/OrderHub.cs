using Microsoft.AspNetCore.SignalR;

public class OrderHub : Hub
{
    public async Task NotifyNewOrder(string orderId)
    {
        await Clients.All.SendAsync("ReceiveNewOrder", orderId);
    }

    public async Task NotifyOrderStatusChange(string orderId, string status)
    {
        await Clients.All.SendAsync("ReceiveOrderStatusChange", orderId, status);
    }
}