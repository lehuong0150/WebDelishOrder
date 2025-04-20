namespace WebDelishOrder.ViewModels
{
   
    public class ReportViewModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string ProductCategory { get; set; }
        public List<RevenueReportItem> RevenueData { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TotalOrders { get; set; }
        public ChartDataViewModel ChartData { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public List<string> ProductCategories { get; set; }
    }

    public class RevenueReportItem
    {
        public int OrderId { get; set; }
        public string OrderCode { get; set; }
        public DateTime OrderDate { get; set; }
        public string CustomerName { get; set; }
        public string ProductName { get; set; }
        public string CategoryName { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Amount { get; set; }
    }

    public class ChartDataViewModel
    {
        public List<string> DailyLabels { get; set; }
        public List<double> DailyData { get; set; }
        public List<string> CategoryLabels { get; set; }
        public List<double> CategoryData { get; set; }
    }

}
