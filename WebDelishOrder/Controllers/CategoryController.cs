using Microsoft.AspNetCore.Mvc;
using WebDelishOrder.Models;
using WebDelishOrder.ViewModels;
using System.Web;
using Microsoft.EntityFrameworkCore;

namespace WebDelishOrder.Controllers
{
    public class CategoryController : Controller
    {
        private readonly AppDbContext _context;

        public CategoryController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index(int page = 1, string searchTerm = "")
        {
            // Thiết lập ViewData để đánh dấu menu
            ViewData["ActivePage"] = "Category"; // Giữ nguyên để đánh dấu menu
            ViewData["PageTitle"] = "Danh mục món ăn"; 

            int pageSize = 6;  // Số sản phẩm mỗi trang
            var query = _context.Categories.AsQueryable();

            // Lọc theo từ khóa tìm kiếm
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(p => p.Name.Contains(searchTerm));
            }

            // Lấy danh sách sản phẩm cho trang hiện tại
            var categories = query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var totalItems = query.Count();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            // Tạo model để truyền vào View
            var model = new CategoryViewModel
            {
                categories = categories,
                CurrentPage = page,
                TotalPages = totalPages,
                SearchTerm = searchTerm
            };

            return View(model);  // Trả về toàn bộ view với dữ liệu phân trang
        }
        public async Task<IActionResult> LoadMenu(string searchTerm, int pageIndex = 1)
        {
            return ViewComponent("CategoryMenu", new { searchTerm = searchTerm, pageIndex = pageIndex });
        }
        [HttpGet]
        public IActionResult GetTotalPages(string searchTerm = "")
        {
            int pageSize = 6; //  Đặt rõ ràng pageSize

            var query = _context.Categories.AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(p => p.Name.Contains(searchTerm));
            }

            int totalItems = query.Count();
            int totalPages = (int)Math.Ceiling((double)totalItems / pageSize); // 👈 ép kiểu double cho chuẩn

            return Json(new { totalPages });
        }


        [HttpPost]
        public IActionResult Create(CategoryViewModel model, IFormFile ImageFile, string ImageUrl)
        {
            var category = model.NewCategory;

            Console.WriteLine($"Category ID: {category.Id}, Category Name: {category.Name}, Is Available: {category.IsAvailable}, Create: {category.CreatedAt}");
            Console.WriteLine($"Image File: {(ImageFile != null ? ImageFile.FileName : "No file uploaded")}");
            Console.WriteLine($"Image URL: {ImageUrl}");

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                foreach (var error in errors)
                {
                    Console.WriteLine("Model Error: " + error);
                }

                model.categories = _context.Categories.ToList();
                return View("Index", model);
            }

            // Xử lý upload ảnh
            if (ImageFile != null && ImageFile.Length > 0)
            {
                string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
                Directory.CreateDirectory(uploadsFolder);

                string fileName = Path.GetFileName(ImageFile.FileName);
                string filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    ImageFile.CopyTo(stream);
                }

                category.ImageCategory = "/uploads/" + fileName;
            }
            else if (!string.IsNullOrEmpty(ImageUrl))
            {
                category.ImageCategory = ImageUrl; // dùng link ảnh nhập tay
            }

            category.CreatedAt = DateTime.Now;

            _context.Categories.Add(category);
            _context.SaveChanges();

            return RedirectToAction("Index");
        }



        // Xử lý khi nhấn "Clear"
        public ActionResult Clear()
        {
            return RedirectToAction("Index");
        }

        // Xử lý khi nhấn "Edit"
        public IActionResult Edit(String id)
        {
            var category = _context.Categories.Find(id);
            if (category == null)
            {
                return NotFound(); // 🔴 Nếu không tìm thấy, trả về lỗi 404
            }

            var model = new CategoryViewModel
            {
                NewCategory = new Category
                {
                    Id = category.Id,
                    Name = category.Name,
                    ImageCategory = category.ImageCategory ?? "", // 🔴 Tránh null
                    IsAvailable = category.IsAvailable
                },
                categories = _context.Categories.ToList()
            };

            return View("Index", model); // 🔥 Đảm bảo trả về View đúng
        }



        [HttpPost]
        public IActionResult Edit(CategoryViewModel model, IFormFile ImageFile)
        {
            var category = model.NewCategory;
            Console.WriteLine($"Category ID: {category.Id}, Category Name: {category.Name}, Is Available: {category.IsAvailable}");
            Console.WriteLine($"Image File: {(ImageFile != null ? ImageFile.FileName : "No file uploaded")}");
            Console.WriteLine($"Current ImageCategory: {category.ImageCategory}"); // Thêm dòng này để theo dõi

            // Tìm category hiện tại trong cơ sở dữ liệu
            var existingCategory = _context.Categories.FirstOrDefault(c => c.Id == category.Id);
            if (existingCategory == null)
            {
                return NotFound("Category not found.");
            }

            // Cập nhật các thuộc tính khác
            existingCategory.Name = category.Name;
            existingCategory.IsAvailable = category.IsAvailable;

            if (ImageFile != null && ImageFile.Length > 0)
            {
                // Xử lý upload ảnh mới
                string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
                Directory.CreateDirectory(uploadsFolder);

                string fileName = Path.GetFileName(ImageFile.FileName);
                string filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    ImageFile.CopyTo(stream);
                }

                // Cập nhật đường dẫn ảnh mới
                existingCategory.ImageCategory = "/uploads/" + fileName;
            }
            else
            {
                // Giữ lại đường dẫn ảnh cũ
                Console.WriteLine($"Keeping original image path: {existingCategory.ImageCategory}");
            }

            // Lưu thay đổi vào cơ sở dữ liệu
            _context.SaveChanges();

            return RedirectToAction("Index");
        }
        // Xử lý khi nhấn "Delete"
        public ActionResult Delete(String id)
        {
            var category = _context.Categories.Find(id);
            if (category != null)
            {
                _context.Categories.Remove(category);
                _context.SaveChanges();
            }
            return RedirectToAction("Index");
        }
    }
}
