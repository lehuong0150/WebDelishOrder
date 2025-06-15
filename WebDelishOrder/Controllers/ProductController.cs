using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebDelishOrder.Models;
using WebDelishOrder.ViewModels;

namespace WebDelishOrder.Controllers
{
    [Authorize(Roles = "ROLE_ADMIN")]

    public class ProductController : Controller
    {
        private readonly AppDbContext _context;

        public ProductController(AppDbContext context)
        {
            _context = context;

        }
     
        public IActionResult Index(int page = 1, string searchTerm = "")
        {
            // Thiết lập ViewData để đánh dấu menu
            ViewData["ActivePage"] = "Product";
            ViewData["PageTitle"] = "Danh sách món ăn";
            int pageSize = 10;  // Số sản phẩm mỗi trang
            var query = _context.Products.AsQueryable();

            // Lọc theo từ khóa tìm kiếm
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(p => p.Name.Contains(searchTerm));
            }

            // Lấy danh sách sản phẩm cho trang hiện tại
            var products = query
                .Include(p => p.Category)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
            foreach (var p in products)
            {
                if (p.Quantity == 0)
                {
                    p.IsAvailable = false;
                }
            }

            var totalItems = query.Count();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            // Tạo model để truyền vào View
            var model = new ProductViewModel
            {
                products = products,
                CurrentPage = page,
                TotalPages = totalPages,
                SearchTerm = searchTerm
            };
            ViewBag.CategoryList = _context.Categories.Select(c => new { c.Id, c.Name }).ToList();

            return View( model);  // Trả về toàn bộ view với dữ liệu phân trang
        }
        public async Task<IActionResult> LoadMenu(string searchTerm, int pageIndex = 1)
        {
            return ViewComponent("ProductMenu", new { searchTerm = searchTerm, pageIndex = pageIndex });
        }
        [HttpGet]
        public IActionResult GetTotalPages(string searchTerm = "")
        {
            int pageSize = 10; //  Đặt rõ ràng pageSize

            var query = _context.Products.AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(p => p.Name.Contains(searchTerm));
            }

            int totalItems = query.Count();
            int totalPages = (int)Math.Ceiling((double)totalItems / pageSize); // 👈 ép kiểu double cho chuẩn

            return Json(new { totalPages });
        }


        // Xử lý khi nhấn "Add"
        [HttpPost]
        public IActionResult Create(ProductViewModel model, IFormFile ImageFile)
        {
            var product = model.NewProduct;

            Console.WriteLine($"Category ID: {product.Id}, Category Name: {product.Name}, Is Available: {product.IsAvailable}, Create: {product.CreatedAt}");
            Console.WriteLine($"Image File: {(ImageFile != null ? ImageFile.FileName : "No file uploaded")}");

            if (!ModelState.IsValid)
            {
                // Log lỗi để debug (nếu cần)
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                foreach (var error in errors)
                {
                    Console.WriteLine("Model Error: " + error);
                }

                // Trả lại view với dữ liệu cũ
                model.products = _context.Products.ToList();
                return View("Index", model);
            }

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

                product.ImageProduct = "/uploads/" + fileName;
            }

            // Không cần kiểm tra và gán ID mới khi thêm, vì ID sẽ tự động được gán khi dữ liệu được lưu vào DB.
            product.CreatedAt = DateTime.Now;

            // Thêm Category mới vào DB
            _context.Products.Add(product);
            _context.SaveChanges();

            // Sau khi thêm thành công, chuyển hướng về trang Index
            return RedirectToAction("Index");
        }


        // Xử lý khi nhấn "Clear"
        public ActionResult Clear()
        {
            return RedirectToAction("Index");
        }

        // Xử lý khi nhấn "Edit"
        public IActionResult Edit( int id)
        {
           
            var product = _context.Products.Find(id);
            if (product == null)
            {
                return NotFound();
            }

            var model = new ProductViewModel
            {
                NewProduct = new Product
                {
                    Id = product.Id,
                    Name = product.Name,
                    Price = product.Price,
                    Descript = product.Descript,
                    Quantity = product.Quantity,
                    ImageProduct = product.ImageProduct ?? "",
                    CategoryId = product.CategoryId,
                    IsAvailable = product.IsAvailable,
                    CreatedAt = product.CreatedAt
                },
                products = _context.Products.ToList()
            };

            // ✅ Chỉ cần đổi ViewBag.CategoryList
            ViewBag.CategoryList = _context.Categories.ToList();

            return View("Index", model);
        }

        [HttpPost]
        public IActionResult Edit(ProductViewModel model, IFormFile ImageFile)
        {
            var product = model.NewProduct;
            Console.WriteLine($"Pro ID: {product.Id}, Pro Name: {product.Name}, Is Available: {product.IsAvailable}");
            Console.WriteLine($"Image File: {(ImageFile != null ? ImageFile.FileName : "No file uploaded")}");
            Console.WriteLine($"Current ImageProduct: {product.ImageProduct}"); // Thêm dòng này

            // Không kiểm tra ModelState.IsValid ngay lập tức
            var existingProduct = _context.Products.Find(model.NewProduct.Id);
            if (existingProduct != null)
            {
                existingProduct.Id = model.NewProduct.Id;
                existingProduct.Name = model.NewProduct.Name;
                existingProduct.Price = model.NewProduct.Price;
                existingProduct.Descript = model.NewProduct.Descript;
                existingProduct.Quantity = model.NewProduct.Quantity;
                existingProduct.CategoryId = model.NewProduct.CategoryId;
                existingProduct.IsAvailable = model.NewProduct.IsAvailable;

                // Xử lý ảnh
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
                    existingProduct.ImageProduct = "/uploads/" + fileName;
                    Console.WriteLine($"New image path: {existingProduct.ImageProduct}");
                }
                else
                {
                    // Giữ nguyên đường dẫn ảnh từ model
                    existingProduct.ImageProduct = product.ImageProduct;
                    Console.WriteLine($"Keeping original image path: {existingProduct.ImageProduct}");
                }

                existingProduct.CreatedAt = model.NewProduct.CreatedAt;
                _context.SaveChanges();
                return RedirectToAction("Index");
            }

            // Nếu có lỗi, thiết lập lại các giá trị và quay lại view
            ViewBag.CategoryList = _context.Categories.ToList();
            model.products = _context.Products.ToList();
            return View("Index",model);
        }

        // Xử lý khi nhấn "Delete"
        public ActionResult Delete(int id)
        {
            var product = _context.Products.Find(id);
            if (product != null)
            {
                _context.Products.Remove(product);
                _context.SaveChanges();
            }
            return RedirectToAction("Index");
        }
    }
}
