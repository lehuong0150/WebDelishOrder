namespace WebDelishOrder.APIControllers
{
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using WebDelishOrder.Models;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using System.Linq;

    [Route("api/[controller]")]
    [ApiController]
    public class ProductApiController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ProductApiController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/ProductApi
        // Lấy danh sách tất cả sản phẩm và bao gồm các quan hệ liên quan (Category, Comments, OrderDetails)
        [HttpGet("all")]
        public async Task<ActionResult<IEnumerable<object>>> GetProducts()
        {
            var products = await _context.Products
                .Include(p => p.Category)  // Bao gồm tên danh mục
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.Price,
                    p.Descript,
                    p.Quantity,
                    p.ImageProduct,
                    p.CategoryId,
                    CategoryName = p.Category.Name,  // Lấy tên danh mục
                    p.IsAvailable,
                    p.CreatedAt
                })
                .ToListAsync();

            return Ok(products);
        }

        // Lấy danh sách sản phẩm theo categoryId
        [HttpGet("category/{categoryId}")]
        public async Task<ActionResult<List<Product>>> GetProductsByCategory(string categoryId)
                {
            var products = await _context.Products
                .Where(p => p.CategoryId == categoryId)
                .ToListAsync();

            if (products == null || !products.Any())
            {
                return NotFound();
            }

            return products;
        }
        
        //tim kiem sp theo ten
        [HttpGet("search")]
        public async Task<ActionResult<List<Product>>> SearchProducts(string keyword)
        {
            if (string.IsNullOrEmpty(keyword))
            {
                return BadRequest("Từ khóa tìm kiếm không hợp lệ.");
            }

            var products = await _context.Products
                .Where(p => p.Name.ToLower().Contains(keyword.ToLower())) 
                .ToListAsync();

            if (products == null || !products.Any())
            {
                return NotFound(); // Trả về lỗi 404 nếu không có sản phẩm nào tìm thấy
            }

            return Ok(products); 
        }

        
        // API riêng cho sắp xếp theo giá
        [HttpGet("sortByPrice")]
        public async Task<ActionResult<IEnumerable<object>>> GetProductsSortedByPrice([FromQuery] string direction = "asc")
        {
            var baseQuery = _context.Products
                .Include(p => p.Category);

            var query = direction?.ToLower() == "desc"
                ? baseQuery.OrderByDescending(p => p.Price)
                : baseQuery.OrderBy(p => p.Price);

            var products = await query
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.Price,
                    p.Descript,
                    p.Quantity,
                    p.ImageProduct,
                    p.CategoryId,
                    CategoryName = p.Category.Name,
                    p.IsAvailable,
                    p.CreatedAt
                })
                .ToListAsync();

            return Ok(products);
        }

        // API riêng cho sắp xếp theo ngày
        [HttpGet("sortByDate")]
        public async Task<ActionResult<IEnumerable<object>>> GetProductsSortedByDate([FromQuery] string direction = "newest")
        {
            var baseQuery = _context.Products
                .Include(p => p.Category);

            var query = direction?.ToLower() == "oldest"
                ? baseQuery.OrderBy(p => p.CreatedAt)
                : baseQuery.OrderByDescending(p => p.CreatedAt);

            var products = await query
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.Price,
                    p.Descript,
                    p.Quantity,
                    p.ImageProduct,
                    p.CategoryId,
                    CategoryName = p.Category.Name,
                    p.IsAvailable,
                    p.CreatedAt
                })
                .ToListAsync();

            return Ok(products);
        }

        // GET: api/ProductApi/5
        // Lấy thông tin chi tiết của sản phẩm dựa trên ID và bao gồm các quan hệ liên quan
        [HttpGet("{id}")]
        public async Task<ActionResult<Product>> GetProductById(int id)
        {
            var product = await _context.Products
            .Include(p => p.Category)
            .Include(p => p.Comments)
            .Include(p => p.OrderDetails)
                    .Select(p => new 
                    {
                        p.Id,
                        p.Name,
                        p.Price,
                        p.Descript,
                        p.Quantity,
                        p.ImageProduct,
                        p.CategoryId,
                        CategoryName = p.Category.Name,  // Lấy tên danh mục
                        p.IsAvailable,
                        p.CreatedAt,
                        Rating = p.Comments.Any() ? p.Comments.Average(c => c.Evaluate) : 0 // Calculate average rating
                    })
            .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                return NotFound();
            }

            return Ok(product);
        }

        // POST: api/ProductApi
        // Thêm mới một sản phẩm vào cơ sở dữ liệu
        [HttpPost]
        public async Task<ActionResult<Product>> PostProduct(Product product)
        {
            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetProductById), new { id = product.Id }, product);
        }

        // PUT: api/ProductApi/5
        // Cập nhật thông tin của sản phẩm dựa trên ID
        [HttpPut("{id}")]
        public async Task<IActionResult> PutProduct(int id, Product product)
        {
            if (id != product.Id)
            {
                return BadRequest();
            }

            _context.Entry(product).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProductExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // DELETE: api/ProductApi/5
        // Xóa sản phẩm khỏi cơ sở dữ liệu
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // Kiểm tra xem sản phẩm có tồn tại trong cơ sở dữ liệu hay không
        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.Id == id);
        }
    }
}
