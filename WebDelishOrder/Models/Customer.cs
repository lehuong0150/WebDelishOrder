using System;
using System.Collections.Generic;

namespace WebDelishOrder.Models;

public partial class Customer
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Phone { get; set; }
    public string Avatar { get; set; } 

    public string? Address { get; set; }

    public string? Gender { get; set; }

    public DateOnly? Birthdate { get; set; }

    public string? AccountEmail { get; set; }

    public virtual Account? AccountEmailNavigation { get; set; }
}
