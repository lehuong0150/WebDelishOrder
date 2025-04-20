using WebDelishOrder.Models;

namespace WebDelishOrder.ViewModels
{
    public class ProductViewModel
    {
        public Product NewProduct { get; set; } = new Product();
        public IEnumerable<Product> products { get; set; } = new List<Product>();
        public int CurrentPage { get; set; } = 1;  // Trang hiện tại
        public int TotalPages { get; set; } = 1;  // Tổng số trang
        public string SearchTerm { get; set; } = string.Empty;  // Từ khóa tìm kiếm
    }
}
