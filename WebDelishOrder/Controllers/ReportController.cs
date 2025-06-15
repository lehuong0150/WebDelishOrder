using DocumentFormat.OpenXml.Drawing.Charts;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Drawing.Printing;
using System.Linq;
using WebDelishOrder.Models;
using WebDelishOrder.ViewModels;
using DinkToPdf;
using DinkToPdf.Contracts;
using OfficeOpenXml.Style;
using OfficeOpenXml;
using System.IO;
using NuGet.Packaging;
using System.ComponentModel;
using Microsoft.AspNetCore.Authorization;

namespace WebDelishOrder.Controllers
{
    [Authorize(Roles = "ROLE_ADMIN")]

    public class ReportController : Controller
    {
        private readonly AppDbContext _context;

        public ReportController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index(DateTime? fromDate, DateTime? toDate, string productCategory = null)
        {
            ViewData["ActivePage"] = "SellingReport";
            ViewData["PageTitle"] = "Báo cáo doanh thu";
            // Default date range: current month
            fromDate ??= new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            toDate ??= DateTime.Now;

            // Query revenue data
            var query = from o in _context.Orders
                        join od in _context.OrderDetails on o.Id equals od.OrderId
                        join p in _context.Products on od.ProductId equals p.Id
                        join c in _context.Categories on p.CategoryId equals c.Id
                        join m in _context.Customers on o.AccountEmail equals m.AccountEmail
                        where o.RegTime >= fromDate && o.RegTime <= toDate
                                && o.Status != 4
                              && (string.IsNullOrEmpty(productCategory) || c.Name == productCategory)
                        select new RevenueReportItem
                        {
                            OrderId = o.Id,
                            OrderDate = o.RegTime.Value,
                            CustomerName = m.Name,
                            ProductName = p.Name,
                            CategoryName = c.Name,
                            Quantity = od.Quantity.Value,
                            UnitPrice = od.Price.Value,
                            Amount = od.Quantity.Value * od.Price.Value
                        };

            var revenueData = query.ToList();

            // Prepare ViewModel
            var viewModel = new ReportViewModel
            {
                FromDate = fromDate.Value,
                ToDate = toDate.Value,
                ProductCategory = productCategory,
                RevenueData = revenueData,
                TotalRevenue = revenueData.Sum(r => r.Amount),
                TotalOrders = revenueData.Select(r => r.OrderId).Distinct().Count(),
                ChartData = GetChartData(revenueData),
                ProductCategories = _context.Categories.Select(c => c.Name).ToList()
            };

            return View(viewModel);
        }

        private ChartDataViewModel GetChartData(List<RevenueReportItem> revenueData)
        {
            // Group revenue by date
            var dailyRevenue = revenueData
                .GroupBy(r => r.OrderDate.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    Amount = g.Sum(r => r.Amount)
                })
                .OrderBy(g => g.Date);

            // Group revenue by category
            var categoryRevenue = revenueData
                .GroupBy(r => r.CategoryName)
                .Select(g => new
                {
                    Category = g.Key,
                    Amount = g.Sum(r => r.Amount)
                })
                .OrderByDescending(g => g.Amount);

            return new ChartDataViewModel
            {
                DailyLabels = dailyRevenue.Select(d => d.Date.ToString("dd/MM/yyyy")).ToList(),
                DailyData = dailyRevenue.Select(d => d.Amount).ToList(),
                CategoryLabels = categoryRevenue.Select(c => c.Category).ToList(),
                CategoryData = categoryRevenue.Select(c => c.Amount).ToList()
            };
        }




        public IActionResult ExportToPdf(DateTime? fromDate, DateTime? toDate, string productCategory = null)
        {
            fromDate ??= new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            toDate ??= DateTime.Now;

            var query = from o in _context.Orders
                        join od in _context.OrderDetails on o.Id equals od.OrderId
                        join p in _context.Products on od.ProductId equals p.Id
                        join m in _context.Customers on o.AccountEmail equals m.AccountEmail
                        join c in _context.Categories on p.CategoryId equals c.Id
                        where o.RegTime >= fromDate && o.RegTime <= toDate
                                && o.Status != 4
                              && (string.IsNullOrEmpty(productCategory) || c.Name == productCategory)
                        select new
                        {
                            OrderId = o.Id,
                            OrderDate = o.RegTime.Value,
                            CustomerName = m.Name,
                            ProductName = p.Name,
                            CategoryName = c.Name,
                            Quantity = od.Quantity.Value,
                            UnitPrice = od.Price.Value,
                            Amount = od.Quantity.Value * od.Price.Value
                        };

            var data = query.ToList();
            var totalAmount = data.Sum(x => x.Amount);

            // Thêm CSS hỗ trợ Unicode và font
            var htmlContent = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <style>
        body {{
            font-family: 'DejaVu Sans', Arial, sans-serif;
            font-size: 12px;
        }}
        h1 {{
            text-align: center;
            color: #333;
        }}
        table {{
            width: 100%;
            border-collapse: collapse;
            margin-top: 20px;
        }}
        th, td {{
            border: 1px solid #333;
            padding: 8px;
            text-align: left;
        }}
        th {{
            background-color: #f2f2f2;
            font-weight: bold;
        }}
        .text-right {{
            text-align: right;
        }}
        .total-row {{
            font-weight: bold;
            background-color: #f9f9f9;
        }}
    </style>
</head>
<body>
    <h1>BÁO CÁO DOANH THU</h1>
    <p><strong>Thời gian:</strong> {fromDate:dd/MM/yyyy} - {toDate:dd/MM/yyyy}</p>
    <table>
        <thead>
            <tr>
                <th>#</th>
                <th>Mã Đơn</th>
                <th>Ngày Đặt</th>
                <th>Khách Hàng</th>
                <th>Sản Phẩm</th>
                <th>Danh Mục</th>
                <th>Số Lượng</th>
                <th>Thành Tiền</th>
            </tr>
        </thead>
        <tbody>";

            int index = 1;
            foreach (var item in data)
            {
                htmlContent += $@"
            <tr>
                <td>{index++}</td>
                <td>{item.OrderId}</td>
                <td>{item.OrderDate:dd/MM/yyyy}</td>
                <td>{item.CustomerName}</td>
                <td>{item.ProductName}</td>
                <td>{item.CategoryName}</td>
                <td class='text-right'>{item.Quantity}</td>
                <td class='text-right'>{item.Amount:N0}</td>
            </tr>";
            }

            htmlContent += $@"
        </tbody>
        <tfoot>
            <tr class='total-row'>
                <td colspan='7' class='text-right'><strong>Tổng Doanh Thu:</strong></td>
                <td class='text-right'><strong>{totalAmount:N0} VND</strong></td>
            </tr>
        </tfoot>
    </table>
</body>
</html>";

            var converter = new SynchronizedConverter(new PdfTools());
            var doc = new HtmlToPdfDocument()
            {
                GlobalSettings = new GlobalSettings
                {
                    ColorMode = DinkToPdf.ColorMode.Color,
                    Orientation = DinkToPdf.Orientation.Portrait,
                    PaperSize = DinkToPdf.PaperKind.A4,
                    Margins = new MarginSettings { Top = 10, Bottom = 10, Left = 10, Right = 10 }
                },
                Objects = {
            new ObjectSettings {
                HtmlContent = htmlContent,
                WebSettings = { DefaultEncoding = "utf-8" },
                HeaderSettings = { FontSize = 9, Right = "Trang [page] / [toPage]", Line = true },
                FooterSettings = { FontSize = 9, Line = true, Left = "Xuất ngày: " + DateTime.Now.ToString("dd/MM/yyyy HH:mm") }
            }
        }
            };

            var pdf = converter.Convert(doc);
            var fileName = $"BaoCaoDoanhThu_{fromDate.Value:yyyyMMdd}_{toDate.Value:yyyyMMdd}.pdf";
            return File(pdf, "application/pdf", fileName);
        }



        public IActionResult ExportToExcel(DateTime? fromDate, DateTime? toDate, string productCategory = null)
        {
            fromDate ??= new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            toDate ??= DateTime.Now;

            var query = from o in _context.Orders
                        join od in _context.OrderDetails on o.Id equals od.OrderId
                        join p in _context.Products on od.ProductId equals p.Id
                        join c in _context.Categories on p.CategoryId equals c.Id
                        join m in _context.Customers on o.AccountEmail equals m.AccountEmail
                        where o.RegTime >= fromDate && o.RegTime <= toDate
                                && o.Status != 4
                              && (string.IsNullOrEmpty(productCategory) || c.Name == productCategory)
                        select new
                        {
                            OrderId = o.Id,
                            OrderDate = o.RegTime.Value,
                            CustomerName = m.Name,
                            ProductName = p.Name,
                            CategoryName = c.Name,
                            Quantity = od.Quantity.Value,
                            UnitPrice = od.Price.Value,
                            Amount = od.Quantity.Value * od.Price.Value
                        };

            var data = query.ToList();
            var totalAmount = data.Sum(x => x.Amount);

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Báo Cáo Doanh Thu");

                // Tiêu đề
                worksheet.Cells["A1"].Value = "BÁO CÁO DOANH THU";
                worksheet.Cells["A1:H1"].Merge = true;
                worksheet.Cells["A1:H1"].Style.Font.Bold = true;
                worksheet.Cells["A1:H1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                // Thời gian
                worksheet.Cells["A2"].Value = $"Thời gian: {fromDate:dd/MM/yyyy} - {toDate:dd/MM/yyyy}";
                worksheet.Cells["A2:H2"].Merge = true;

                // Header
                worksheet.Cells[4, 1].Value = "STT";
                worksheet.Cells[4, 2].Value = "Mã Đơn";
                worksheet.Cells[4, 3].Value = "Ngày Đặt";
                worksheet.Cells[4, 4].Value = "Khách Hàng";
                worksheet.Cells[4, 5].Value = "Sản Phẩm";
                worksheet.Cells[4, 6].Value = "Danh Mục";
                worksheet.Cells[4, 7].Value = "Số Lượng";
                worksheet.Cells[4, 8].Value = "Thành Tiền";
                worksheet.Cells["A4:H4"].Style.Font.Bold = true;

                // Data
                int row = 5;
                int index = 1;
                foreach (var item in data)
                {
                    worksheet.Cells[row, 1].Value = index++;
                    worksheet.Cells[row, 2].Value = item.OrderId;
                    worksheet.Cells[row, 3].Value = item.OrderDate.ToString("dd/MM/yyyy");
                    worksheet.Cells[row, 4].Value = item.CustomerName;
                    worksheet.Cells[row, 5].Value = item.ProductName;
                    worksheet.Cells[row, 6].Value = item.CategoryName;
                    worksheet.Cells[row, 7].Value = item.Quantity;
                    worksheet.Cells[row, 8].Value = item.Amount;
                    row++;
                }

                // Tổng doanh thu
                worksheet.Cells[row, 1, row, 7].Merge = true;
                worksheet.Cells[row, 1].Value = "Tổng Doanh Thu";
                worksheet.Cells[row, 1].Style.Font.Bold = true;
                worksheet.Cells[row, 8].Value = totalAmount;
                worksheet.Cells[row, 8].Style.Font.Bold = true;

                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                var stream = new MemoryStream();
                package.SaveAs(stream);
                stream.Position = 0;
                var fileName = $"BaoCaoDoanhThu_{fromDate.Value:yyyyMMdd}_{toDate.Value:yyyyMMdd}.xlsx";
                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
        }
    }
}
