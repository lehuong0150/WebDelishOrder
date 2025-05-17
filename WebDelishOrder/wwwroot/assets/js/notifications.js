
const connection = new signalR.HubConnectionBuilder()
        .withUrl("/orderHub")
        .build();

    let soundEnabled = true;
    const notificationSound = new Audio("~/assets/sounds/notification.mp3");

    // Toggle sound
    function toggleSound() {
        soundEnabled = !soundEnabled;
        alert(soundEnabled ? "Âm thanh đã bật" : "Âm thanh đã tắt");
    }

    // Handle new order notifications
    connection.on("ReceiveNewOrder", function (orderId) {
        // Update badge
        const badge = document.getElementById("newOrderBadge");
        badge.textContent = parseInt(badge.textContent) + 1;

        // Show notification bell
        const notificationBell = document.getElementById("notificationBell");
        notificationBell.classList.add("active");

        // Play sound
        if (soundEnabled) {
            notificationSound.play();
        }

        // Show toast notification
        const toast = document.createElement("div");
        toast.className = "toast-notification";
        toast.innerHTML = `Đơn hàng mới: <strong>${orderId}</strong>`;
        document.body.appendChild(toast);
        setTimeout(() => toast.remove(), 5000);
    });

    // Handle order status change notifications
    connection.on("ReceiveOrderStatusChange", function (orderId, status) {
        alert(`Trạng thái đơn hàng ${orderId} đã thay đổi thành: ${status}`);
    });

    // Start SignalR connection
    connection.start().catch(function (err) {
        console.error(err.toString());
    });

    // Auto-refresh page every 1-5 minutes
    setInterval(() => {
        location.reload();
    }, 300000); // 5 minutes

