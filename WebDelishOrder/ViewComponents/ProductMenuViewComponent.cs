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
        int pageSize = 10;
        var products = _context.Products
            .Include(p => p.Category)
            .Where(p => string.IsNullOrEmpty(searchTerm) || p.Name.Contains(searchTerm))
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToList();
        foreach (var p in products)
        {
            if (p.Quantity == 0)
            {
                p.IsAvailable = false;
            }
        }
        _context.SaveChanges();
        return View(products);
    }
}
