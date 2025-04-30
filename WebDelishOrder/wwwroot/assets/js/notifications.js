// Kết nối đến SignalR Hub
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/notificationHub")
    .withAutomaticReconnect()
    .build();

// Bắt đầu kết nối
connection.start().then(function () {
    console.log("SignalR Connected!");

    // Đăng ký người dùng hiện tại với hub
    if (currentUserId) {
        connection.invoke("RegisterUser", currentUserId);
    }

    // Nếu là admin, tham gia vào nhóm admin
    if (isAdmin) {
        connection.invoke("JoinGroup", "Admins");
    }

}).catch(function (err) {
    console.error(err.toString());
});

// Xử lý khi nhận được thông báo đơn hàng mới
connection.on("ReceiveNewOrder", function (orderId, orderDetails) {
    // Hiển thị badge thông báo mới
    updateNotificationBadge();

    // Phát âm thanh nếu tính năng được bật
    if (soundEnabled) {
        playNotificationSound();
    }

    // Hiển thị thông báo ở góc màn hình
    showToastNotification(`Đơn hàng mới #${orderId}`, orderDetails);
});

// Xử lý khi nhận thông báo chung
connection.on("ReceiveNotification", function (title, message, type) {
    showToastNotification(title, message, type);
});

// Xử lý khi trạng thái đơn hàng thay đổi
connection.on("OrderStatusChanged", function (orderId, newStatus) {
    // Cập nhật UI nếu đơn hàng hiện đang được hiển thị
    updateOrderStatusInUI(orderId, newStatus);

    // Hiển thị thông báo
    showToastNotification("Cập nhật đơn hàng", `Đơn hàng #${orderId} đã chuyển sang trạng thái: ${newStatus}`);
});