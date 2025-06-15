using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using WebDelishOrder.Models;

namespace WebDelishOrder.Controllers
{
    [Authorize(Roles = "ROLE_ADMIN")]
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;

        public HomeController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /Home/Index
        public IActionResult Index()
        {
            // Thi?t l?p ViewData ?? ?ánh d?u menu
            ViewData["ActivePage"] = "Dashboard";
            ViewData["PageTitle"] = "Tổng quan";

            // Tính toán các ch? s? th?ng kê
            CalculateKPIs();

            // L?y d? li?u cho các bi?u ??
            PrepareChartData();

            // L?y ??n hàng g?n ?ây
            GetRecentOrders();

            // L?y món ?n ph? bi?n
            GetPopularItems();

            return View();
        }

        // GET: /Home/GetRevenueData
        [HttpGet]
        public IActionResult GetRevenueData(string period = "day")
        {
            var today = DateTime.Today;
            var labels = new List<string>();
            var values = new List<double>();

            switch (period)
            {
                case "day":
                    // Doanh thu 30 ngày g?n nh?t
                    for (int i = 29; i >= 0; i--)
                    {
                        var date = today.AddDays(-i);
                        labels.Add(date.ToString("dd/MM"));

                        var revenue = _context.Orders
                            .Where(o => o.RegTime.HasValue && o.RegTime.Value.Date == date && o.Status != 4)
                            .Sum(o => o.TotalPrice) ?? 0;

                        values.Add((double)revenue);
                    }
                    break;

                case "week":
                    // Doanh thu 12 tu?n g?n nh?t
                    for (int i = 11; i >= 0; i--)
                    {
                        var startOfWeek = today.AddDays(-(int)today.DayOfWeek).AddDays(-7 * i);
                        var endOfWeek = startOfWeek.AddDays(6);

                        labels.Add($"{startOfWeek:dd/MM} - {endOfWeek:dd/MM}");

                        var revenue = _context.Orders
                            .Where(o => o.RegTime.HasValue &&
                                   o.RegTime.Value.Date >= startOfWeek &&
                                   o.RegTime.Value.Date <= endOfWeek &&
                                   o.Status != 4)
                            .Sum(o => o.TotalPrice) ?? 0;

                        values.Add((double)revenue);
                    }
                    break;

                case "month":
                    // Doanh thu 12 tháng g?n nh?t
                    for (int i = 11; i >= 0; i--)
                    {
                        var date = today.AddMonths(-i);
                        var firstDayOfMonth = new DateTime(date.Year, date.Month, 1);
                        var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);

                        labels.Add(date.ToString("MM/yyyy"));

                        var revenue = _context.Orders
                            .Where(o => o.RegTime.HasValue &&
                                   o.RegTime.Value.Date >= firstDayOfMonth &&
                                   o.RegTime.Value.Date <= lastDayOfMonth &&
                                   o.Status != 4)
                            .Sum(o => o.TotalPrice) ?? 0;

                        values.Add((double)revenue);
                    }
                    break;
            }

            return Json(new { labels, values });
        }

        // Ph??ng th?c tính toán các KPI
        private void CalculateKPIs()
        {
            var today = DateTime.Today;
            var firstDayOfMonth = new DateTime(today.Year, today.Month, 1);
            var lastDayOfLastMonth = firstDayOfMonth.AddDays(-1);
            var firstDayOfLastMonth = new DateTime(lastDayOfLastMonth.Year, lastDayOfLastMonth.Month, 1);

            // Tính t?ng doanh thu tháng này
            var revenueThisMonth = _context.Orders
                .Where(o => o.RegTime.HasValue &&
                       o.RegTime.Value.Date >= firstDayOfMonth &&
                       o.Status != 4)
                .Sum(o => o.TotalPrice) ?? 0;

            // Tính t?ng doanh thu tháng tr??c
            var revenueLastMonth = _context.Orders
                .Where(o => o.RegTime.HasValue &&
                       o.RegTime.Value.Date >= firstDayOfLastMonth &&
                       o.RegTime.Value.Date < firstDayOfMonth &&
                       o.Status != 4)
                .Sum(o => o.TotalPrice) ?? 0;

            // Tính % t?ng tr??ng doanh thu
            double revenueIncrease = 0;
            if (revenueLastMonth > 0)
            {
                revenueIncrease = Math.Round((double)(revenueThisMonth - revenueLastMonth) * 100 / (double)revenueLastMonth, 1);
            }
            else if (revenueThisMonth > 0)
            {
                revenueIncrease = 100;
            }

            // ??m t?ng s? ??n hàng tháng này
            var ordersThisMonth = _context.Orders
                .Count(o => o.RegTime.HasValue && o.RegTime.Value.Date >= firstDayOfMonth);

            // ??m t?ng s? ??n hàng tháng tr??c
            var ordersLastMonth = _context.Orders
                .Count(o => o.RegTime.HasValue &&
                       o.RegTime.Value.Date >= firstDayOfLastMonth &&
                       o.RegTime.Value.Date < firstDayOfMonth);

            // Tính % t?ng tr??ng s? ??n hàng
            double ordersIncrease = 0;
            if (ordersLastMonth > 0)
            {
                ordersIncrease = Math.Round((double)(ordersThisMonth - ordersLastMonth) * 100 / (double)ordersLastMonth, 1);
            }
            else if (ordersThisMonth > 0)
            {
                ordersIncrease = 100;
            }

            // ??m s? khách hàng ho?t ??ng tháng này (khách hàng có ??t ??n hàng)
            var activeCustomersThisMonth = _context.Orders
                .Where(o => o.RegTime.HasValue && o.RegTime.Value.Date >= firstDayOfMonth)
                .Select(o => o.AccountEmail)
                .Distinct()
                .Count();

            // ??m s? khách hàng ho?t ??ng tháng tr??c
            var activeCustomersLastMonth = _context.Orders
                .Where(o => o.RegTime.HasValue &&
                       o.RegTime.Value.Date >= firstDayOfLastMonth &&
                       o.RegTime.Value.Date < firstDayOfMonth)
                .Select(o => o.AccountEmail)
                .Distinct()
                .Count();

            // Tính % t?ng tr??ng khách hàng ho?t ??ng
            double customersIncrease = 0;
            if (activeCustomersLastMonth > 0)
            {
                customersIncrease = Math.Round((double)(activeCustomersThisMonth - activeCustomersLastMonth) * 100 / (double)activeCustomersLastMonth, 1);
            }
            else if (activeCustomersThisMonth > 0)
            {
                customersIncrease = 100;
            }

            // ??m ??n hàng ch? x? lý
            var pendingOrders = _context.Orders.Count(o => o.Status == 0);

            // ??a k?t qu? vào ViewBag
            ViewBag.TotalRevenue = revenueThisMonth;
            ViewBag.RevenueIncrease = revenueIncrease;
            ViewBag.TotalOrders = ordersThisMonth;
            ViewBag.OrdersIncrease = ordersIncrease;
            ViewBag.NewCustomers = activeCustomersThisMonth; // ??i t? NewCustomers thành s? khách hàng ho?t ??ng
            ViewBag.CustomersIncrease = customersIncrease;
            ViewBag.PendingOrders = pendingOrders;
        }

        // Ph??ng th?c chu?n b? d? li?u cho các bi?u ??
        private void PrepareChartData()
        {
            var today = DateTime.Today;

            // D? li?u bi?u ?? doanh thu (30 ngày g?n nh?t)
            var labels = Enumerable.Range(0, 30)
                .Select(i => today.AddDays(-i))
                .Reverse()
                .Select(d => d.ToString("dd/MM"))
                .ToArray();

            var values = Enumerable.Range(0, 30)
                .Select(i => today.AddDays(-i))
                .Reverse()
                .Select(d => {
                    var revenue = _context.Orders
                        .Where(o => o.RegTime.HasValue && o.RegTime.Value.Date == d && o.Status != 4)
                        .Sum(o => o.TotalPrice) ?? 0;
                    return (double)revenue;
                })
                .ToArray();

            ViewBag.RevenueData = new { labels, values };

            // D? li?u bi?u ?? tr?ng thái ??n hàng
            var statusLabels = new[] { "Ch? xác nh?n", "?ang chu?n b?", "?ang giao hàng", "?ã giao hàng", "?ã h?y" };
            var statusValues = Enumerable.Range(0, 5)
                .Select(status => _context.Orders.Count(o => o.Status == status))
                .ToArray();

            ViewBag.OrderStatusData = new { labels = statusLabels, values = statusValues };

            // D? li?u bi?u ?? khung gi? ??t hàng
            var timeLabels = new[] { "0-4h", "4-8h", "8-12h", "12-16h", "16-20h", "20-24h" };
            var timeValues = new int[6];

            // Nhóm ??n hàng theo khung gi?
            var orders = _context.Orders
                .Where(o => o.RegTime.HasValue && o.RegTime.Value.Date >= today.AddDays(-30))
                .ToList();

            foreach (var order in orders)
            {
                if (order.RegTime.HasValue)
                {
                    int hourGroup = order.RegTime.Value.Hour / 4;
                    if (hourGroup >= 0 && hourGroup < 6)
                    {
                        timeValues[hourGroup]++;
                    }
                }
            }

            ViewBag.OrderTimeData = new { labels = timeLabels, values = timeValues };

            // D? li?u bi?u ?? doanh thu theo danh m?c
            var categoryData = _context.OrderDetails
                .Include(od => od.Product)
                .ThenInclude(p => p.Category)
                .Where(od => od.Order.Status != 4 &&
                       od.Order.RegTime.HasValue &&
                       od.Order.RegTime.Value.Date >= today.AddDays(-30))
                .GroupBy(od => od.Product.Category.Name)
                .Select(g => new
                {
                    Category = g.Key,
                    Revenue = g.Sum(od => od.Product.Price * od.Quantity)
                })
                .OrderByDescending(x => x.Revenue)
                .Take(5)
                .ToList();

            var categoryLabels = categoryData.Select(c => c.Category).ToArray();
            var categoryValues = categoryData.Select(c => (double)c.Revenue).ToArray();

            ViewBag.CategoryRevenueData = new { labels = categoryLabels, values = categoryValues };
        }

        // Ph??ng th?c l?y ??n hàng g?n ?ây
        private void GetRecentOrders()
        {
            var recentOrders = _context.Orders
                .OrderByDescending(o => o.RegTime)
                .Take(5)
                .Join(_context.Customers,
                      order => order.AccountEmail,
                      customer => customer.AccountEmail,
                      (order, customer) => new
                      {
                          Id = order.Id,
                          CustomerName = customer.Name ?? order.AccountEmail,
                          OrderDate = order.RegTime ?? DateTime.Now,
                          TotalAmount = order.TotalPrice ?? 0,
                          Status = order.Status ?? 0
                      })
                .ToList();

            // Trong tr??ng h?p không có kh?p, c?n ki?m tra
            if (recentOrders.Count < 5)
            {
                var missingOrders = _context.Orders
                    .OrderByDescending(o => o.RegTime)
                    .Take(5)
                    .Where(o => !recentOrders.Select(ro => ro.Id).Contains(o.Id))
                    .Select(o => new
                    {
                        Id = o.Id,
                        CustomerName = o.AccountEmail, // Không tìm th?y Customer
                        OrderDate = o.RegTime ?? DateTime.Now,
                        TotalAmount = o.TotalPrice ?? 0,
                        Status = o.Status ?? 0
                    })
                    .ToList();

                recentOrders = recentOrders.Concat(missingOrders).OrderByDescending(o => o.OrderDate).Take(5).ToList();
            }

            ViewBag.RecentOrders = recentOrders;
        }

        // Ph??ng th?c l?y món ?n ph? bi?n
        private void GetPopularItems()
        {
            var popularItems = _context.OrderDetails
                .GroupBy(od => od.ProductId)
                .Select(g => new
                {
                    ProductId = g.Key,
                    OrderCount = g.Sum(od => od.Quantity)
                })
                .OrderByDescending(x => x.OrderCount)
                .Take(5)
                .Join(_context.Products,
                      oi => oi.ProductId,
                      p => p.Id,
                      (oi, p) => new
                      {
                          Id = p.Id,
                          Name = p.Name,
                          Price = p.Price,
                          ImageUrl = p.ImageProduct, // S? d?ng tên tr??ng ImageProduct
                          OrderCount = oi.OrderCount
                      })
                .ToList();

            ViewBag.PopularItems = popularItems;
        }
    }
}
