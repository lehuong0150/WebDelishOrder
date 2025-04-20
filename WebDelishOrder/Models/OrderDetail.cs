using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace WebDelishOrder.Models;

public partial class OrderDetail
{
    public int OrderId { get; set; }

    public int ProductId { get; set; }

    public float? Price { get; set; }

    public int? Quantity { get; set; }

    [JsonIgnore]
    public virtual Order? Order { get; set; } = null!;
    [JsonIgnore]
    public virtual Product? Product { get; set; } = null!;
}
