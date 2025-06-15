using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using WebDelishOrder.Models;
using WebDelishOrder.ViewModels;

namespace WebDelishOrder.Controllers
{
    [Authorize(Roles = "ROLE_ADMIN")]
    public class ReviewController : Controller
    {
        private readonly AppDbContext _context;

        public ReviewController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Review
        public IActionResult Index(string searchEmail = "", int? rating = null)
        {
            ViewData["ActivePage"] = "ReviewFood";
            ViewData["PageTitle"] = "Đánh giá món ăn";

            var commentsQuery = _context.Comments
                .Include(c => c.Product)
                .AsQueryable();

            // Lọc theo email nếu có
            if (!string.IsNullOrEmpty(searchEmail))
            {
                commentsQuery = commentsQuery.Where(c => c.AccountEmail.Contains(searchEmail));
            }

            // Lọc theo đánh giá nếu có
            if (rating.HasValue)
            {
                commentsQuery = commentsQuery.Where(c => c.Evaluate == rating);
            }

            // Lấy dữ liệu và map sang ViewModel
            var result = commentsQuery
                .OrderByDescending(c => c.RegTime)
                .Select(c => new CommentViewModel
                {
                    AccountEmail = c.AccountEmail,
                    ProductName = c.Product != null ? c.Product.Name : "(Không rõ)",
                    Evaluate = c.Evaluate,
                    Descript = c.Descript,
                    RegTime = c.RegTime
                })
                .ToList();

            return View(result);
        }
    }
}
