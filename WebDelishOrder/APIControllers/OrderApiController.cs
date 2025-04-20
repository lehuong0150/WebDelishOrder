using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebDelishOrder.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using WebDelishOrder.Hubs;
using WebDelishOrder.Service;

[ApiController]
[Route("api/[controller]")]
public class OrderApiController : ControllerBase
{
    private readonly AppDbContext _context;

    public OrderApiController(AppDbContext context)
    {
        _context = context;
    }

    // ROLE_CUSTOMER: Xem danh sách đơn hàng của chính mình
   //[Authorize(Roles = "ROLE_CUSTOMER")]
    // GET: api/OrderApi?email=user@example.com&status=all/0,1,2/4
    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> GetOrders(string email, string status = "all")
    {
        if (string.IsNullOrEmpty(email))
        {
            return BadRequest("Email is required");
        }

        // Query to get orders by email
        var query = _context.Orders
    .Where(o => o.AccountEmail == email)
    .Join(
        _context.Accounts, // Join with the Accounts table
        order => order.AccountEmail, // Foreign key in Orders
        account => account.Email,   // Primary key in Accounts
        (order, account) => new // Select the desired fields
        {
            order.Id,
            order.ShippingAddress,
            order.Phone,
            order.RegTime,
            order.Status,
            order.AccountEmail,
            NameCustomer = account.Fullname, // Fetch the customer's name
            order.PaymentMethod,
            order.PaymentStatus,
            order.TotalPrice,
            // Để tạm thời null, sẽ xác định sau khi có thông tin OrderDetail
            isRate = (bool?)null,
            OrderDetails = _context.OrderDetails
                .Where(od => od.OrderId == order.Id)
                .Select(od => new
                {
                    od.OrderId,
                    od.ProductId,
                    ProductName = od.Product.Name,
                    imageProduct = od.Product.ImageProduct,
                    od.Quantity,
                    od.Price,
                    // Kiểm tra từng sản phẩm đã được đánh giá chưa
                    isRated = _context.Comments
                        .Any(c => c.ProductId == od.ProductId &&
                                 c.AccountEmail == email)
                }).ToList()
        });

        // Filter by status if provided
        if (!string.IsNullOrEmpty(status) && status != "all")
        {
            if (int.TryParse(status, out int singleStatus))
            {
                query = query.Where(o => o.Status == singleStatus);
            }
            else
            {
                var statusList = status.Split(',')
                    .Select(s => int.TryParse(s.Trim(), out int val) ? (int?)val : null)
                    .Where(s => s.HasValue)
                    .Select(s => s.Value);

                if (statusList.Any())
                {
                    query = query.Where(o => o.Status.HasValue && statusList.Contains(o.Status.Value));
                }
            }
        }

        // Fetch the results
        var ordersTemp = await query
            .OrderByDescending(o => o.RegTime)
            .ToListAsync();

        // Xử lý thêm để xác định isRate dựa trên isRated của tất cả các sản phẩm
        var orders = ordersTemp.Select(o => new
        {
            o.Id,
            o.ShippingAddress,
            o.Phone,
            o.RegTime,
            o.Status,
            o.AccountEmail,
            o.NameCustomer,
            o.PaymentMethod,
            o.PaymentStatus,
            o.TotalPrice,
            // Chỉ true khi TẤT CẢ sản phẩm đều đã được đánh giá
            isRate = o.OrderDetails.Count > 0 && o.OrderDetails.All(od => od.isRated),
            o.OrderDetails
        }).ToList();

        return Ok(orders);
    }
    [HttpGet("{id}")]
    public async Task<ActionResult<object>> GetOrderById(int id)
    {
        if (id <= 0)
        {
            return BadRequest("Invalid order ID");
        }

        // Query to get order by ID
        var query = _context.Orders
            .Where(o => o.Id == id)
            .Join(
                _context.Accounts, // Join with the Accounts table
                order => order.AccountEmail, // Foreign key in Orders
                account => account.Email,   // Primary key in Accounts
                (order, account) => new // Select the desired fields
                {
                    order.Id,
                    order.ShippingAddress,
                    order.Phone,
                    order.RegTime,
                    order.Status,
                    order.AccountEmail,
                    NameCustomer = account.Fullname, // Fetch the customer's name
                    order.PaymentMethod,
                    order.PaymentStatus,
                    order.TotalPrice,
                    // Để tạm thời null, sẽ xác định sau khi có thông tin OrderDetail
                    isRate = (bool?)null,
                    OrderDetail = _context.OrderDetails
                        .Where(od => od.OrderId == order.Id)
                        .Select(od => new
                        {
                            od.OrderId,
                            od.ProductId,
                            ProductName = od.Product.Name,
                            imageProduct = od.Product.ImageProduct,
                            od.Quantity,
                            od.Price,
                            // Kiểm tra từng sản phẩm đã được đánh giá chưa
                            isRated = _context.Comments
                                .Any(c => c.ProductId == od.ProductId &&
                                         c.AccountEmail == order.AccountEmail)
                        }).ToList()
                });

        // SingleOrDefaultAsync để lấy một đơn hàng hoặc null nếu không tìm thấy
        var orderTemp = await query.SingleOrDefaultAsync();

        // Kiểm tra nếu không tìm thấy đơn hàng
        if (orderTemp == null)
        {
            return NotFound($"Order with ID {id} not found");
        }

        // Xử lý thêm để xác định isRate dựa trên isRated của tất cả các sản phẩm
        var order = new
        {
            orderTemp.Id,
            orderTemp.ShippingAddress,
            orderTemp.Phone,
            orderTemp.RegTime,
            orderTemp.Status,
            orderTemp.AccountEmail,
            orderTemp.NameCustomer,
            orderTemp.PaymentMethod,
            orderTemp.PaymentStatus,
            orderTemp.TotalPrice,
            // Chỉ true khi TẤT CẢ sản phẩm đều đã được đánh giá
            isRate = orderTemp.OrderDetail.Count > 0 && orderTemp.OrderDetail.All(od => od.isRated),
            orderTemp.OrderDetail
        };

        return Ok(order);
    }

    [HttpGet("current")]
    public async Task<ActionResult<IEnumerable<object>>> GetCurrentOrders(string email)
    {
        if (string.IsNullOrEmpty(email))
        {
            return BadRequest("Email is required");
        }

        // Giả sử các trạng thái đơn hàng đang xử lý: 0=Pending, 1=Processing, 2=Shipping
        var pendingStatuses = new[] { 0, 1, 2 };

        var pendingOrders = await _context.Orders
            .Where(o => o.AccountEmail == email && o.Status.HasValue && pendingStatuses.Contains(o.Status.Value))
            .OrderByDescending(o => o.RegTime)
            .Select(o => new
            {
                o.Id,
                o.ShippingAddress,
                o.Phone,
                o.Status,
                o.RegTime,
                o.TotalPrice,
                o.PaymentMethod,
                o.PaymentStatus,
                Items = _context.OrderDetails
                    .Where(od => od.OrderId == o.Id)
                    .Join(_context.Products,
                        orderDetail => orderDetail.ProductId,
                        product => product.Id,
                        (orderDetail, product) => new
                        {
                            OrderId = orderDetail.OrderId,
                            ProductId = orderDetail.ProductId,
                            ProductName = product.Name,
                            ProductImage = product.ImageProduct,
                            Price = orderDetail.Price,
                            Quantity = orderDetail.Quantity
                        }).ToList()
            })
            .ToListAsync();

        return Ok(pendingOrders);
    }

    //Tao don hang
    [HttpPost("orders")]
    public IActionResult CreateOrder([FromBody] Order order)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Gán thời gian nếu client không gửi lên
        if (order.RegTime == null)
        {
            order.RegTime = DateTime.Now;
        }

        // Kiểm tra đơn hàng có chi tiết hay không
        if (order.OrderDetails == null || order.OrderDetails.Count == 0)
        {
            return BadRequest("Đơn hàng phải có ít nhất 1 món.");
        }

        try
        {
            _context.Orders.Add(order);
            _context.SaveChanges();
            return Ok(order);
        }
        catch (Exception ex)
        {
            return StatusCode(500, "Lỗi server: " + ex.Message);
        }
    }


    // ROLE_ADMIN: Xóa bất kỳ đơn hàng nào
    [Authorize(Roles = "ROLE_ADMIN")]
    [HttpDelete("admin/{id}")]
    public async Task<IActionResult> AdminDeleteOrder(int id)
    {
        var existingOrder = await _context.Orders.FindAsync(id);
        if (existingOrder == null)
        {
            return NotFound();
        }

        // Xóa tất cả OrderDetails liên quan trước
        var orderDetails = await _context.OrderDetails.Where(od => od.OrderId == id).ToListAsync();
        _context.OrderDetails.RemoveRange(orderDetails);

        // Sau đó xóa Order
        _context.Orders.Remove(existingOrder);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // Thêm endpoint lọc theo ngày
    [HttpGet("filter")]
    public async Task<ActionResult<IEnumerable<object>>> GetOrdersByDateRange(
        string email,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] string status = "all")
    {
        if (string.IsNullOrEmpty(email))
        {
            return BadRequest("Email is required");
        }

        // Tạo Dictionary chứa các sản phẩm đã đánh giá
        var reviewedProductsDict = await _context.Comments
            .Where(c => c.AccountEmail == email && c.ProductId.HasValue)
            .ToDictionaryAsync(c => c.ProductId.Value, c => true);

        var query = _context.Orders.Where(o => o.AccountEmail == email);

        // Lọc theo trạng thái
        if (!string.IsNullOrEmpty(status) && status != "all")
        {
            if (int.TryParse(status, out int singleStatus))
            {
                query = query.Where(o => o.Status == singleStatus);
            }
            else
            {
                var statusList = status.Split(',')
                    .Select(s => int.TryParse(s.Trim(), out int val) ? (int?)val : null)
                    .Where(s => s.HasValue)
                    .Select(s => s.Value);

                if (statusList.Any())
                {
                    query = query.Where(o => o.Status.HasValue && statusList.Contains(o.Status.Value));
                }
            }
        }

        // Lọc theo ngày bắt đầu
        if (startDate.HasValue)
        {
            query = query.Where(o => o.RegTime.HasValue && o.RegTime >= startDate);
        }

        // Lọc theo ngày kết thúc
        if (endDate.HasValue)
        {
            // Đặt thời gian là cuối ngày
            var endOfDay = endDate.Value.Date.AddDays(1).AddTicks(-1);
            query = query.Where(o => o.RegTime.HasValue && o.RegTime <= endOfDay);
        }

        var filteredOrders = await query
            .OrderByDescending(o => o.RegTime)
            .Select(o => new
            {
                o.Id,
                o.ShippingAddress,
                o.Phone,
                o.Status,
                o.RegTime,
                o.TotalPrice,
                o.PaymentMethod,
                o.PaymentStatus,
                Items = _context.OrderDetails
                    .Where(od => od.OrderId == o.Id)
                    .Join(_context.Products,
                        orderDetail => orderDetail.ProductId,
                        product => product.Id,
                        (orderDetail, product) => new
                        {
                            OrderId = orderDetail.OrderId,
                            ProductId = orderDetail.ProductId,
                            ProductName = product.Name,
                            ProductImage = product.ImageProduct,
                            Price = orderDetail.Price,
                            Quantity = orderDetail.Quantity
                        }).ToList()
            })
            .ToListAsync();

        // Tạo kết quả mới với IsReviewed
        var result = filteredOrders.Select(order => new
        {
            order.Id,
            order.ShippingAddress,
            order.Phone,
            order.Status,
            order.RegTime,
            order.TotalPrice,
            order.PaymentMethod,
            order.PaymentStatus,
            Items = order.Items.Select(item => new
            {
                item.OrderId,
                item.ProductId,
                item.ProductName,
                item.ProductImage,
                item.Price,
                item.Quantity,
                IsReviewed = reviewedProductsDict.ContainsKey(item.ProductId)
            }).ToList()
        }).ToList();

        return Ok(result);
    }

    // Thêm endpoint để kiểm tra đơn hàng theo OrderCode
    [HttpGet("code/{orderCode}")]
    public async Task<ActionResult<object>> GetOrderByCode(string orderCode, string email)
    {
        if (string.IsNullOrEmpty(orderCode))
        {
            return BadRequest("Order code is required");
        }

        if (string.IsNullOrEmpty(email))
        {
            return BadRequest("Email is required");
        }

        // Tạo Dictionary chứa các sản phẩm đã đánh giá
        var reviewedProductsDict = await _context.Comments
            .Where(c => c.AccountEmail == email && c.ProductId.HasValue)
            .ToDictionaryAsync(c => c.ProductId.Value, c => true);

        var order = await _context.Orders
            .Where(o => o.AccountEmail == email && o.ShippingAddress == orderCode) // Giả sử orderCode được lưu trong ShippingAddress
            .Select(o => new
            {
                o.Id,
                o.ShippingAddress,
                o.Phone,
                o.Status,
                o.RegTime,
                o.TotalPrice,
                o.PaymentMethod,
                o.PaymentStatus,
                Items = _context.OrderDetails
                    .Where(od => od.OrderId == o.Id)
                    .Join(_context.Products,
                        orderDetail => orderDetail.ProductId,
                        product => product.Id,
                        (orderDetail, product) => new
                        {
                            OrderId = orderDetail.OrderId,
                            ProductId = orderDetail.ProductId,
                            ProductName = product.Name,
                            ProductImage = product.ImageProduct,
                            Price = orderDetail.Price,
                            Quantity = orderDetail.Quantity
                        }).ToList()
            })
            .FirstOrDefaultAsync();

        if (order == null)
        {
            return NotFound("Order not found");
        }

        // Tạo kết quả mới với IsReviewed
        var result = new
        {
            order.Id,
            order.ShippingAddress,
            order.Phone,
            order.Status,
            order.RegTime,
            order.TotalPrice,
            order.PaymentMethod,
            order.PaymentStatus,
            Items = order.Items.Select(item => new
            {
                item.OrderId,
                item.ProductId,
                item.ProductName,
                item.ProductImage,
                item.Price,
                item.Quantity,
                IsReviewed = reviewedProductsDict.ContainsKey(item.ProductId)
            }).ToList()
        };

        return Ok(result);
    }

    // Kiểm tra sản phẩm đã được đánh giá chưa
    [HttpGet("check-commented")]
    public async Task<ActionResult<bool>> CheckProductCommented(string email, int productId)
    {
        if (string.IsNullOrEmpty(email))
        {
            return BadRequest("Email is required");
        }

        bool isCommented = await _context.Comments
            .AnyAsync(c => c.AccountEmail == email && c.ProductId == productId);

        return Ok(isCommented);
    }


    [HttpPost("admin/confirm/{id}")]
    public async Task<IActionResult> ConfirmOrder(int id, [FromServices] OrderHubService hubService)
    {
        var order = await _context.Orders.FindAsync(id);
        if (order == null)
        {
            return NotFound("Order not found");
        }

        // Cập nhật trạng thái đơn hàng
        order.Status = 1; // Ví dụ: Confirmed
        await _context.SaveChangesAsync();

        // Gọi service để gửi thông báo SignalR
        await hubService.NotifyOrderStatusChange(order.Id, "Confirmed");

        return Ok("Order confirmed");
    }
}
