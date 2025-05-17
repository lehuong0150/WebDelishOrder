using System;
using System.Collections.Generic;

namespace WebDelishOrder.Models;

public partial class Role
{
    public string Id { get; set; } = null!;

    public string? Name { get; set; }

    public virtual ICollection<Account> AccountEmails { get; set; } = new List<Account>();

}
