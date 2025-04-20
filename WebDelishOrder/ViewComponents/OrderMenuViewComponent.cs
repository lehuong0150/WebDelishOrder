using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebDelishOrder.Models;

namespace WebDelishOrder.ViewComponents
{
    public class OrderMenuViewComponent:ViewComponent
    {
        private readonly AppDbContext _context;

        public OrderMenuViewComponent(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync(string searchTerm, string status = "all", string sort = "desc", int pageIndex = 1)
        {
            int pageSize = 6;
            var query = _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .AsQueryable();

            // Lọc theo từ khóa tìm kiếm
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(o =>
                    (o.ShippingAddress != null && o.ShippingAddress.Contains(searchTerm)) ||
                    (o.AccountEmail != null && o.AccountEmail.Contains(searchTerm)));
            }

            // Lọc theo trạng thái
            if (!string.IsNullOrEmpty(status) && status != "all")
            {
                int statusValue = int.Parse(status);
                query = query.Where(o => o.Status == statusValue);
            }

            // Sắp xếp theo thời gian
            if (sort == "asc")
            {
                query = query.OrderBy(o => o.RegTime);
            }
            else
            {
                query = query.OrderByDescending(o => o.RegTime);
            }

            var orders = await query
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            if (orders == null || !orders.Any())
            {
                Console.WriteLine($"No orders found with searchTerm={searchTerm}, status={status}, sort={sort}, pageIndex={pageIndex}");
            }

            var customerNames = new Dictionary<int, string>();
            foreach (var order in orders)
            {
                // Kiểm tra nếu AccountEmail là null
                if (order.AccountEmail == null)
                {
                    customerNames[order.Id] = "Unknown";
                    continue;
                }

                try
                {
                    var customer = await _context.Customers
                        .FirstOrDefaultAsync(c => c.AccountEmail == order.AccountEmail);
                    customerNames[order.Id] = customer?.Name ?? "Unknown";
                }
                catch (Exception ex)
                {
                    // Ghi lại lỗi và tiếp tục với khách hàng không xác định
                    Console.WriteLine($"Error finding customer for order {order.Id}: {ex.Message}");
                    customerNames[order.Id] = "Unknown";
                }
            }

            ViewBag.CustomerNames = customerNames;
            return View(orders);
        }
    }
}
