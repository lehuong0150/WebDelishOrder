using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebDelishOrder.Models;

public class ProductMenuViewComponent : ViewComponent
{
    private readonly AppDbContext _context;

    public ProductMenuViewComponent(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IViewComponentResult> InvokeAsync(string searchTerm, int pageIndex)
    {
        int pageSize = 6;
        var products = _context.Products
            .Include(p => p.Category)
            .Where(p => string.IsNullOrEmpty(searchTerm) || p.Name.Contains(searchTerm))
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return View(products);
    }
}
