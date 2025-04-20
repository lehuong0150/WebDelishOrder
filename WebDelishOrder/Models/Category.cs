using System;
using System.Collections.Generic;

namespace WebDelishOrder.Models;

public partial class Category
{
    public string Id { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? ImageCategory { get; set; }

    public bool IsAvailable { get; set; } = true; 

    public DateTime? CreatedAt { get; set; } = DateTime.Now; // Ngày tạo


    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
