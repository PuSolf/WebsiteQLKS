using System;
using System.Collections.Generic;

namespace WebsiteQLKS.Models;

public partial class Role
{
    public int RoleId { get; set; }

    public string Ten { get; set; } = null!;

    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
