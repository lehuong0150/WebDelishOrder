using System;
using System.Collections.Generic;

namespace WebDelishOrder.Models;

public partial class Comment
{
    public string AccountEmail { get; set; } = null!;

    public int? ProductId { get; set; }

    public DateTime RegTime { get; set; }

    public string? Descript { get; set; }

    public int? Evaluate { get; set; }

    public virtual Account AccountEmailNavigation { get; set; } = null!;

    public virtual Product? Product { get; set; }
}
