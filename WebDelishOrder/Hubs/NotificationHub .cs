using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace WebDelishOrder.Hubs
{
    public class NotificationHub : Hub
    {
        // Dictionary lưu trữ mapping giữa UserID và ConnectionID
        private static Dictionary<string, List<string>> _userConnections = new Dictionary<string, List<string>>();

        // Kết nối mới
        public override async Task OnConnectedAsync()
        {
            await base.OnConnectedAsync();
            await Clients.Caller.SendAsync("Connected", Context.ConnectionId);
        }

        // Ngắt kết nối
        public override async Task OnDisconnectedAsync(Exception exception)
        {
            // Tìm và xóa connectionId khỏi tất cả user có connection này
            foreach (var userId in _userConnections.Keys.ToList())
            {
                if (_userConnections[userId].Contains(Context.ConnectionId))
                {
                    _userConnections[userId].Remove(Context.ConnectionId);

                    // Nếu user không còn connection nào, xóa user khỏi dictionary
                    if (_userConnections[userId].Count == 0)
                    {
                        _userConnections.Remove(userId);
                    }
                }
            }

            await base.OnDisconnectedAsync(exception);
        }

        // Đăng ký UserID với ConnectionID
        public async Task RegisterUser(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                return;

            // Nếu userId chưa có trong dictionary, thêm mới
            if (!_userConnections.ContainsKey(userId))
            {
                _userConnections[userId] = new List<string>();
            }

            // Thêm connectionId vào danh sách connection của user
            if (!_userConnections[userId].Contains(Context.ConnectionId))
            {
                _userConnections[userId].Add(Context.ConnectionId);
            }

            await Task.CompletedTask;
        }

        // Gửi thông báo đơn hàng mới đến tất cả quản trị viên
        public async Task SendNewOrderNotification(int orderId, string orderDetails)
        {
            await Clients.All.SendAsync("ReceiveNewOrder", orderId, orderDetails);
        }

        // Gửi thông báo đến một người dùng cụ thể
        public async Task SendNotificationToUser(string userId, string title, string message, string type)
        {
            if (_userConnections.ContainsKey(userId))
            {
                foreach (var connectionId in _userConnections[userId])
                {
                    await Clients.Client(connectionId).SendAsync("ReceiveNotification", title, message, type);
                }
            }
        }

        // Gửi thông báo cập nhật trạng thái đơn hàng đến người dùng
        public async Task SendOrderStatusUpdate(string userId, int orderId, string newStatus)
        {
            if (_userConnections.ContainsKey(userId))
            {
                foreach (var connectionId in _userConnections[userId])
                {
                    await Clients.Client(connectionId).SendAsync("OrderStatusChanged", orderId, newStatus);
                }
            }
        }

        // Gửi thông báo đến tất cả các quản trị viên có vai trò xử lý đơn hàng
        public async Task NotifyAdmins(string message, string notificationType)
        {
            // Giả sử có một nhóm "Admins" được cấu hình
            await Clients.Group("Admins").SendAsync("AdminNotification", message, notificationType);
        }

        // Tham gia vào nhóm (ví dụ: nhóm quản trị viên)
        public async Task JoinGroup(string groupName)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        }

        // Rời khỏi nhóm
        public async Task LeaveGroup(string groupName)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        }
    }
}
