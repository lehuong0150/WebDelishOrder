using WebDelishOrder.Models;

namespace WebDelishOrder.ViewModels
{
    public class CategoryViewModel
    {
        public Category NewCategory { get; set; } = new Category();
        public IEnumerable<Category> categories { get; set; } = new List<Category>();
        public int CurrentPage { get; set; } = 1;  // Trang hiện tại
        public int TotalPages { get; set; } = 1;  // Tổng số trang
        public string SearchTerm { get; set; } = string.Empty;  // Từ khóa tìm kiếm
    }
}
