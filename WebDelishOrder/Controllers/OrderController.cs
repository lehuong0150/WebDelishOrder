using DocumentFormat.OpenXml.InkML;
using FirebaseAdmin.Messaging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using WebDelishOrder.Models;
using WebDelishOrder.ViewModels;
using FirebaseAdmin.Messaging;
using Microsoft.AspNetCore.Authorization;

namespace WebDelishOrder.Controllers
{
    [Authorize(Roles = "ROLE_ADMIN")]

    public class OrderController : Controller
    {
        private readonly AppDbContext _context;

        // Constructor to initialize the DbContext
        public OrderController(AppDbContext context)
        {
            _context = context;
        }


        // Display the list of orders with pagination and search functionality
        public IActionResult Index(int page = 1, string searchTerm = "", string status = "all", string sort = "desc")
        {
            ViewData["ActivePage"] = "UpdateStatus";
            ViewData["PageTitle"] = "Cập nhật đơn hàng";
            int pageSize = 10; // Number of orders per page

            // Truy vấn Orders trước
            var ordersQuery = _context.Orders.AsQueryable();

            // Lọc theo từ khóa tìm kiếm
            if (!string.IsNullOrEmpty(searchTerm))
            {
                ordersQuery = ordersQuery.Where(o =>
                    (o.ShippingAddress != null && o.ShippingAddress.Contains(searchTerm)) ||
                    (o.AccountEmail != null && o.AccountEmail.Contains(searchTerm)) ||
                    (o.Phone != null && o.Phone.Contains(searchTerm)));
            }

            // Lọc theo trạng thái đơn
            if (!string.IsNullOrEmpty(status) && status != "all")
            {
                int statusValue = int.Parse(status);
                ordersQuery = ordersQuery.Where(o => o.Status == statusValue);
            }

            // Sắp xếp theo thời gian đặt hàng
            if (sort == "asc")
            {
                ordersQuery = ordersQuery.OrderBy(o => o.RegTime);
            }
            else
            {
                ordersQuery = ordersQuery.OrderByDescending(o => o.RegTime);
            }

            // Tính tổng số mục và trang
            var totalItems = ordersQuery.Count();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            // Lấy danh sách Order cho trang hiện tại
            var orders = ordersQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // Tạo Dictionary để lưu tên khách hàng
            var customerNames = new Dictionary<int, string>();

            // Lấy tên khách hàng cho mỗi đơn hàng một cách an toàn
            foreach (var order in orders)
            {
                try
                {
                    if (order.AccountEmail != null)
                    {
                        var customer = _context.Customers
                            .FirstOrDefault(c => c.AccountEmail == order.AccountEmail);

                        customerNames[order.Id] = customer?.Name ?? "Không xác định";
                    }
                    else
                    {
                        customerNames[order.Id] = "Không xác định";
                    }
                }
                catch
                {
                    customerNames[order.Id] = "Không xác định";
                }
            }

            // Truyền dữ liệu qua ViewBag
            ViewBag.CustomerNames = customerNames;

            // Tạo ViewModel để truyền dữ liệu vào View
            var model = new OrderViewModel
            {
                orders = orders,
                CurrentPage = page,
                TotalPages = totalPages,
                SearchTerm = searchTerm,
                Status = status,
                Sort = sort
            };

            return View(model);
        }

        // Load the order menu dynamically using a ViewComponent
        public async Task<IActionResult> LoadMenu(string searchTerm, string status = "all", string sort = "desc", int pageIndex = 1)
        {
            try
            {
                Console.WriteLine($"LoadMenu called with searchTerm: {searchTerm}, pageIndex: {pageIndex}");
                return await Task.FromResult(ViewComponent("OrderMenu", new { searchTerm = searchTerm, status = status, sort = sort, pageIndex = pageIndex }));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in LoadMenu: {ex.Message}");
                return StatusCode(500, "Internal Server Error");
            }
        }

        // Get the total number of pages for pagination
        [HttpGet]
        public IActionResult GetTotalPages(string searchTerm = "", string status = "all", string sort = "desc")
        {
            int pageSize = 10; // Number of orders per page

            var query = _context.Orders.AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(o =>
                    (o.ShippingAddress != null && o.ShippingAddress.Contains(searchTerm)) ||
                    (o.AccountEmail != null && o.AccountEmail.Contains(searchTerm)) ||
                    (o.Phone != null && o.Phone.Contains(searchTerm)));
            }
            // Apply status filter
            if (!string.IsNullOrEmpty(status) && status != "all")
            {
                if (int.TryParse(status, out int statusValue))
                {
                    query = query.Where(o => o.Status == statusValue);
                }
            }

            int totalItems = query.Count();
            int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            return Json(new { totalPages });
        }

        // GET: Edit Order Status
        [HttpGet]
        public IActionResult Edit(int id)
        {
            var order = _context.Orders.FirstOrDefault(o => o.Id == id);
            if (order == null)
            {
                return NotFound("Order not found.");
            }

            return View(order);
        }

        [HttpPost]
        public IActionResult UpdateOrder(Order updatedOrder)
        {
            // Find the order by ID
            var order = _context.Orders.Include(o => o.OrderDetails).FirstOrDefault(o => o.Id == updatedOrder.Id);
            if (order == null)
            {
                return NotFound("Order not found.");
            }

            // Update the order details
            order.Status = updatedOrder.Status;
            order.ShippingAddress = updatedOrder.ShippingAddress;
            order.Phone = updatedOrder.Phone;
            order.PaymentMethod = updatedOrder.PaymentMethod;
            order.PaymentStatus = updatedOrder.PaymentStatus;

            // Save changes to the database
            _context.SaveChanges();

            TempData["SuccessMessage"] = "Order updated successfully.";

            // Gửi thông báo qua Firebase Cloud Messaging (FCM)
            var customer = _context.Accounts.FirstOrDefault(a => a.Email == order.AccountEmail);
            Console.WriteLine("Customer Firebase Token: " + customer.FirebaseToken);
            Console.WriteLine("Email: " + order.AccountEmail);
            if (customer != null && !string.IsNullOrEmpty(customer.FirebaseToken))
            {
                var message = new FirebaseAdmin.Messaging.Message()
                {
                    Notification = new FirebaseAdmin.Messaging.Notification()
                    {
                        Title = "Cập nhật trạng thái đơn hàng",
                        Body = $"Đơn hàng #{order.Id} đã được cập nhật trạng thái mới: {GetOrderStatusText(order.Status)}"
                    },
                    Data = new Dictionary<string, string>()
                    {
                        { "title", "Cập nhật trạng thái đơn hàng" },
                        { "body", $"Đơn hàng #{order.Id} đã được cập nhật trạng thái mới: {GetOrderStatusText(order.Status)}" },
                        { "orderId", order.Id.ToString() },
                        { "status", order.Status.ToString() }
                    },
                    Token = customer.FirebaseToken
                };

                try
                {
                    string response = FirebaseAdmin.Messaging.FirebaseMessaging.DefaultInstance.SendAsync(message).Result;
                    Console.WriteLine("Successfully sent FCM message: " + response);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error sending FCM message: " + ex.Message);
                }
            }


            // Get the referrer (the page that submitted the form)
            string referrer = Request.Headers["Referer"].ToString();
            if (!string.IsNullOrEmpty(referrer))
            {
                return Redirect(referrer);
            }

            // Fallback to Index if referrer is not available
            return RedirectToAction("Index");
        }
        // Delete an order by its ID
        public ActionResult Delete(int id)
        {
            var order = _context.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefault(o => o.Id == id);

            if (order != null)
            {
                // Remove related order details first
                _context.OrderDetails.RemoveRange(order.OrderDetails);

                // Remove the order
                _context.Orders.Remove(order);
                _context.SaveChanges();
            }

            return RedirectToAction("Index");
        }
        //THong bao don hàng moi
        [HttpGet]
        public IActionResult CheckNewOrders(DateTime? lastCheckTime)
        {
            var newOrders = _context.Orders
                .Where(o => o.RegTime > lastCheckTime)
                .Select(o => new { o.Id, o.RegTime })
                .ToList();

            return Json(newOrders);
        }
        public IActionResult PrintInvoice(int id)
        {
            var order = _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product) // Include the Product entity
                .FirstOrDefault(o => o.Id == id);

            if (order == null)
            {
                return NotFound("Order not found.");
            }

            // Lấy tên khách hàng từ bảng Customers
            if (string.IsNullOrEmpty(order.AccountEmail))
            {
                ViewBag.CustomerName = "Không xác định";
            }
            else
            {
                var customer = _context.Customers
                 .Select(c => new
                 {
                     c.Id,
                     c.Name,
                     c.AccountEmail,
                     Avatar = c.Avatar ?? "default-avatar.png" // Handle NULL values
                 })
                 .FirstOrDefault(c => c.AccountEmail == order.AccountEmail);

                ViewBag.CustomerName = customer?.Name ?? "Không xác định";
            }

            return View("Invoice", order);
        }



        // Action Details trong OrderController
        public IActionResult Details(int id)
        {
            ViewData["ActivePage"] = "UpdateStatus";
            ViewData["PageTitle"] = "Chi tiết đơn hàng";

            var order = _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .ThenInclude(p => p.Category)
                .FirstOrDefault(o => o.Id == id);

            if (order == null)
            {
                return NotFound("Không tìm thấy đơn hàng.");
            }

            // Lấy thông tin khách hàng từ bảng Customer
            var customer = _context.Customers
                .FirstOrDefault(c => c.AccountEmail == order.AccountEmail);

            // Tạo lịch sử đơn hàng giả lập
            var orderHistories = new List<OrderHistoryDisplay>();

            // Thêm trạng thái hiện tại vào lịch sử
            string statusText;
            string badgeClass;
            string statusIcon;

            switch (order.Status)
            {
                case 0:
                    statusText = "Chờ xác nhận";
                    badgeClass = "bg-warning";
                    statusIcon = "icofont icofont-clock-time";
                    break;
                case 1:
                    statusText = "Đang chuẩn bị";
                    badgeClass = "bg-info";
                    statusIcon = "icofont icofont-cooking";
                    break;
                case 2:
                    statusText = "Đang giao hàng";
                    badgeClass = "bg-primary";
                    statusIcon = "icofont icofont-delivery-time";
                    break;
                case 3:
                    statusText = "Đã giao hàng";
                    badgeClass = "bg-success";
                    statusIcon = "icofont icofont-check-circled";
                    break;
                case 4:
                    statusText = "Đã hủy";
                    badgeClass = "bg-danger";
                    statusIcon = "icofont icofont-close-circled";
                    break;
                default:
                    statusText = "Không xác định";
                    badgeClass = "bg-secondary";
                    statusIcon = "icofont icofont-question";
                    break;
            }

            orderHistories.Add(new OrderHistoryDisplay
            {
                Title = statusText,
                Timestamp = DateTime.Now,
                Description = $"Đơn hàng có trạng thái hiện tại là {statusText}",
                BadgeClass = badgeClass,
                Icon = statusIcon
            });

            // Thêm trạng thái đặt hàng ban đầu
            orderHistories.Add(new OrderHistoryDisplay
            {
                Title = "Đơn hàng được tạo",
                Timestamp = order.RegTime ?? DateTime.Now.AddDays(-1),
                Description = $"Đơn hàng #{order.Id} đã được tạo bởi {order.AccountEmail}",
                BadgeClass = "bg-primary",
                Icon = "icofont icofont-shopping-cart"
            });

            var viewModel = new OrderDetailViewModel
            {
                Order = order,
                Customer = customer,
                OrderHistories = orderHistories
            };

            return View(viewModel);
        }
        private string GetOrderStatusText(int? status)
        {
            return status switch
            {
                0 => "Chờ xác nhận",
                1 => "Đang chuẩn bị",
                2 => "Đang giao",
                3 => "Đã giao",
                4 => "Đã hủy",

            };
        }
    }

}
