using WebDelishOrder.Models;

namespace WebDelishOrder.ViewModels
{
    public class OrderViewModel
    {
        public Order NewOrder { get; set; } = new Order();
        public IEnumerable<Order> orders { get; set; } = new List<Order>();
        public int CurrentPage { get; set; } = 1;  // Trang hiện tại
        public int TotalPages { get; set; } = 1;  // Tổng số trang
        public string SearchTerm { get; set; } = string.Empty;  // Từ khóa tìm kiếm
        public string Status { get; set; } = "all";  // Trạng thái lọc (mặc định là "all")
        public string Sort { get; set; } = "desc";  // Sắp xếp (mặc định là "desc")
        public Dictionary<int, string> CustomerNames { get; set; } = new Dictionary<int, string>();
        public IEnumerable<OrderViewModel> Orders { get; set; }
    }
    public class OrderDetailViewModel
    {
        public Order Order { get; set; }
        public Customer Customer { get; set; }
        public List<OrderHistoryDisplay> OrderHistories { get; set; }
    }

    public class OrderHistoryDisplay
    {
        public string Title { get; set; }
        public DateTime Timestamp { get; set; }
        public string Description { get; set; }
        public string BadgeClass { get; set; }
        public string Icon { get; set; }
    }
}
