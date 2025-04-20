using Microsoft.AspNetCore.Mvc;
using WebDelishOrder.Models;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace WebDelishOrder.ViewComponents
{
    public class CategoryMenuViewComponent : ViewComponent
    {
        private readonly AppDbContext _context;

        public CategoryMenuViewComponent(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync(string searchTerm, int pageIndex)
        {
            if (pageIndex < 1)
            {
                pageIndex = 1;
            }
            int pageSize = 5;
            var categories = _context.Categories
                .Where(p => string.IsNullOrEmpty(searchTerm) || p.Name.Contains(searchTerm))
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return View(categories);
        }
    }
}
