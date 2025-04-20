using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ClosedXML.Excel;
using WebDelishOrder.Models;
using WebDelishOrder.ViewModels; // Thay bằng namespace thực tế của bạn

namespace WebDelishOrder.Controllers
{
    public class ReportController : Controller
    {
        private readonly AppDbContext _context;

        public ReportController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index(DateTime? fromDate, DateTime? toDate, string productCategory = null, int? page = 1)
        {
            ViewData["ActivePage"] = "SellingReport"; // Giữ nguyên để đánh dấu menu
            ViewData["PageTitle"] = "Báo cáo doanh thu";

            // Nếu không có ngày được chọn, mặc định lấy báo cáo tháng hiện tại
            if (!fromDate.HasValue)
                fromDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);

            if (!toDate.HasValue)
                toDate = DateTime.Now;

            // Truy vấn dữ liệu doanh thu
            var query = GetRevenueData(fromDate.Value, toDate.Value, productCategory);

            // Phân trang
            int pageSize = 20;
            var paginatedData = query.Skip((page.Value - 1) * pageSize).Take(pageSize).ToList();

            // Tạo view model
            var viewModel = new ReportViewModel
            {
                FromDate = fromDate.Value,
                ToDate = toDate.Value,
                ProductCategory = productCategory,
                RevenueData = paginatedData,
                TotalRevenue = query.Sum(r => r.Amount),
                TotalOrders = query.Select(r => r.OrderId).Distinct().Count(),
                // Thêm dữ liệu cho biểu đồ
                ChartData = GetChartData(fromDate.Value, toDate.Value, productCategory),
                // Thêm thông tin phân trang
                CurrentPage = page.Value,
                TotalPages = (int)Math.Ceiling(query.Count() / (double)pageSize),
                // Danh sách danh mục sản phẩm để làm bộ lọc
                ProductCategories = GetProductCategories()
            };

            return View(viewModel);
        }

        // Action xuất báo cáo doanh thu ra Excel sử dụng ClosedXML
        public IActionResult ExportToExcel(DateTime fromDate, DateTime toDate, string productCategory = null)
        {
            // Lấy dữ liệu báo cáo
            var revenueData = GetRevenueData(fromDate, toDate, productCategory).ToList();

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Báo cáo doanh thu");

                // Tạo tiêu đề
                worksheet.Cell(1, 1).Value = "BÁO CÁO DOANH THU";
                worksheet.Range(1, 1, 1, 9).Merge();
                worksheet.Cell(1, 1).Style.Font.Bold = true;
                worksheet.Cell(1, 1).Style.Font.FontSize = 16;
                worksheet.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                // Thông tin thời gian
                worksheet.Cell(2, 1).Value = $"Từ ngày: {fromDate.ToShortDateString()} đến ngày: {toDate.ToShortDateString()}";
                worksheet.Range(2, 1, 2, 9).Merge();
                worksheet.Cell(2, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                if (!string.IsNullOrEmpty(productCategory))
                {
                    worksheet.Cell(3, 1).Value = $"Danh mục: {productCategory}";
                    worksheet.Range(3, 1, 3, 9).Merge();
                    worksheet.Cell(3, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                }

                // Header cho bảng dữ liệu
                int headerRow = string.IsNullOrEmpty(productCategory) ? 4 : 5;
                worksheet.Cell(headerRow, 1).Value = "STT";
                worksheet.Cell(headerRow, 2).Value = "Mã đơn hàng";
                worksheet.Cell(headerRow, 3).Value = "Ngày đặt hàng";
                worksheet.Cell(headerRow, 4).Value = "Khách hàng";
                worksheet.Cell(headerRow, 5).Value = "Sản phẩm";
                worksheet.Cell(headerRow, 6).Value = "Danh mục";
                worksheet.Cell(headerRow, 7).Value = "Số lượng";
                worksheet.Cell(headerRow, 8).Value = "Đơn giá";
                worksheet.Cell(headerRow, 9).Value = "Thành tiền";

                // Định dạng header
                var headerRange = worksheet.Range(headerRow, 1, headerRow, 9);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
                headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                headerRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

                // Thêm dữ liệu vào worksheet
                int startRow = headerRow + 1;
                for (int i = 0; i < revenueData.Count; i++)
                {
                    int row = startRow + i;
                    worksheet.Cell(row, 1).Value = i + 1; // STT
                    worksheet.Cell(row, 2).Value = revenueData[i].OrderCode;
                    worksheet.Cell(row, 3).Value = revenueData[i].OrderDate;
                    worksheet.Cell(row, 4).Value = revenueData[i].CustomerName;
                    worksheet.Cell(row, 5).Value = revenueData[i].ProductName;
                    worksheet.Cell(row, 6).Value = revenueData[i].CategoryName;
                    worksheet.Cell(row, 7).Value = revenueData[i].Quantity;
                    worksheet.Cell(row, 8).Value = revenueData[i].UnitPrice;
                    worksheet.Cell(row, 9).Value = revenueData[i].Amount;

                    // Định dạng cột ngày tháng
                    worksheet.Cell(row, 3).Style.DateFormat.Format = "dd/MM/yyyy";

                    // Định dạng cột tiền tệ
                    worksheet.Cell(row, 8).Style.NumberFormat.Format = "#,##0";
                    worksheet.Cell(row, 9).Style.NumberFormat.Format = "#,##0";
                }

                // Border cho bảng dữ liệu
                worksheet.Range(startRow, 1, startRow + revenueData.Count - 1, 9).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                worksheet.Range(startRow, 1, startRow + revenueData.Count - 1, 9).Style.Border.InsideBorder = XLBorderStyleValues.Thin;

                // Tổng doanh thu ở cuối bảng
                int totalRow = startRow + revenueData.Count;
                worksheet.Range(totalRow, 1, totalRow, 8).Merge();
                worksheet.Cell(totalRow, 1).Value = "TỔNG CỘNG:";
                worksheet.Cell(totalRow, 1).Style.Font.Bold = true;
                worksheet.Cell(totalRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                worksheet.Cell(totalRow, 9).Value = revenueData.Sum(r => r.Amount);
                worksheet.Cell(totalRow, 9).Style.Font.Bold = true;
                worksheet.Cell(totalRow, 9).Style.NumberFormat.Format = "#,##0";

                // Border cho dòng tổng
                worksheet.Range(totalRow, 1, totalRow, 9).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                worksheet.Cell(totalRow, 9).Style.Border.BottomBorder = XLBorderStyleValues.Double;

                // Thông tin thời gian xuất báo cáo
                worksheet.Cell(totalRow + 2, 7).Value = $"Ngày xuất: {DateTime.Now.ToString("dd/MM/yyyy HH:mm")}";
                worksheet.Range(totalRow + 2, 7, totalRow + 2, 9).Merge();
                worksheet.Cell(totalRow + 2, 7).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                // Tự động điều chỉnh độ rộng cột
                worksheet.Columns().AdjustToContents();

                // Tạo file Excel và trả về cho người dùng
                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    stream.Position = 0;

                    string fileName = $"BaoCaoDoanhThu_{fromDate:yyyyMMdd}_den_{toDate:yyyyMMdd}.xlsx";
                    return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
            }
        }

        // Action in báo cáo (chuyển tới trang in)
        public IActionResult Print(DateTime fromDate, DateTime toDate, string productCategory = null)
        {
            var revenueData = GetRevenueData(fromDate, toDate, productCategory).ToList();
            var viewModel = new ReportViewModel
            {
                FromDate = fromDate,
                ToDate = toDate,
                ProductCategory = productCategory,
                RevenueData = revenueData,
                TotalRevenue = revenueData.Sum(r => r.Amount),
                TotalOrders = revenueData.Select(r => r.OrderId).Distinct().Count()
            };

            return View("Print", viewModel);
        }

        // Phương thức hỗ trợ lấy dữ liệu doanh thu từ database
        private List<RevenueReportItem> GetRevenueData(DateTime fromDate, DateTime toDate, string productCategory = null)
        {
            // Truy vấn dữ liệu từ các bảng thực tế trong database của bạn
            // Ví dụ: Bạn có thể điều chỉnh tên bảng, tên cột phù hợp với cấu trúc DB của bạn
            var query = from o in _context.Orders // Điều chỉnh tên bảng Order
                        join od in _context.OrderDetails on o.Id equals od.OrderId // Điều chỉnh tên bảng OrderDetail
                        join p in _context.Products on od.ProductId equals p.Id // Điều chỉnh tên bảng Product
                        join c in _context.Categories on p.CategoryId equals c.Id // Điều chỉnh tên bảng Category
                        join cus in _context.Customers on o.Id equals cus.Id // Điều chỉnh tên bảng Customer
                        where o.RegTime >= fromDate.Date && o.RegTime <= toDate.Date.AddDays(1).AddSeconds(-1)
                              && o.Status == 3 // Điều chỉnh trạng thái theo DB của bạn
                              && (string.IsNullOrEmpty(productCategory) || c.Name == productCategory)
                        select new RevenueReportItem
                        {
                            OrderId = o.Id,
                            OrderDate = o.RegTime.Value,
                            CustomerName = cus.Name, // Điều chỉnh tên cột
                            ProductName = p.Name,
                            CategoryName = c.Name,
                            Quantity = od.Quantity.Value,
                            UnitPrice = (decimal)od.Price.Value, // Điều chỉnh tên cột
                            Amount = (decimal)o.TotalPrice.Value
                        };

            return query.ToList();
        }

        // Phương thức lấy dữ liệu cho biểu đồ
        private ChartDataViewModel GetChartData(DateTime fromDate, DateTime toDate, string productCategory = null)
        {
            // Dữ liệu doanh thu theo ngày
            var dailyRevenue = from o in _context.Orders
                               join od in _context.OrderDetails on o.Id equals od.OrderId
                               join p in _context.Products on od.ProductId equals p.Id
                               join c in _context.Categories on p.CategoryId equals c.Id
                               where o.RegTime >= fromDate.Date && o.RegTime <= toDate.Date.AddDays(1).AddSeconds(-1)
                                     && o.Status == 3 // Điều chỉnh trạng thái
                                     && (string.IsNullOrEmpty(productCategory) || c.Name == productCategory)
                               group new { o, od } by new { Date = o.RegTime } into g
                               select new
                               {
                                   Date = g.Key.Date,
                                   Amount = g.Sum(x => x.od.Quantity * x.od.Price) // Điều chỉnh tên cột
                               };

            // Dữ liệu doanh thu theo danh mục sản phẩm
            var categoryRevenue = from o in _context.Orders
                                  join od in _context.OrderDetails on o.Id equals od.OrderId
                                  join p in _context.Products on od.ProductId equals p.Id
                                  join c in _context.Categories on p.CategoryId equals c.Id
                                  where o.RegTime >= fromDate.Date && o.RegTime <= toDate.Date.AddDays(1).AddSeconds(-1)
                                        && o.Status == 3 // Điều chỉnh trạng thái
                                        && (string.IsNullOrEmpty(productCategory) || c.Name == productCategory)
                                  group new { od } by new { Category = c.Name } into g
                                  select new
                                  {
                                      Category = g.Key.Category,
                                      Amount = g.Sum(x => x.od.Quantity * x.od.Price) // Điều chỉnh tên cột
                                  };

            var dailyRevenueList = dailyRevenue.OrderBy(d => d.Date).ToList();
            var categoryRevenueList = categoryRevenue.OrderByDescending(c => c.Amount).ToList();

            return new ChartDataViewModel
            {
                DailyLabels = dailyRevenueList.Select(d => d.Date.Value.Day + "/" + d.Date.Value.Month).ToList(),
                DailyData = dailyRevenueList.Select(d => (double)d.Amount).ToList(),
                CategoryLabels = categoryRevenueList.Select(c => c.Category).ToList(),
                CategoryData = categoryRevenueList.Select(c => (double)c.Amount).ToList()
            };
        }

        // Phương thức lấy danh sách danh mục sản phẩm
        private List<string> GetProductCategories()
        {
            return _context.Categories // Điều chỉnh tên bảng
                  .Select(c => c.Name) // Điều chỉnh tên cột
                  .OrderBy(c => c)
                  .ToList();
        }
    }
}
