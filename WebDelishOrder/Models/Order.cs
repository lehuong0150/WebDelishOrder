using System;
using System.Collections.Generic;

namespace WebDelishOrder.Models;

public partial class Order
{
    public int Id { get; set; }

    public string? ShippingAddress { get; set; }

    public string? Phone { get; set; }

    public DateTime? RegTime { get; set; }

    public int? Status { get; set; }

    public string? AccountEmail { get; set; }

    public string PaymentMethod { get; set; } = null!;

    public string? PaymentStatus { get; set; }

    public float? TotalPrice { get; set; }

    public virtual Account? AccountEmailNavigation { get; set; }

    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
}
