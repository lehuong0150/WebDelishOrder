using System;
using System.Collections.Generic;

namespace WebDelishOrder.Models;

public partial class Product
{
    public int Id { get; set; }

    public string? Name { get; set; }

    public float? Price { get; set; }

    public string? Descript { get; set; }
    public int? Quantity { get; set; }
    public string? ImageProduct { get; set; }

    public string? CategoryId { get; set; }

    public bool IsAvailable { get; set; }

    public DateTime? CreatedAt { get; set; }  // Ngày tạo

    public virtual Category? Category { get; set; }

    public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();

    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();

   
}
