using System;
using System.Collections.Generic;

namespace BookNumAPI.Models;

public partial class ActivityCategory
{
    public int CategoryId { get; set; }

    public string CategoryName { get; set; } = null!;

    public bool IsActive { get; set; }

    public virtual ICollection<Activity> Activities { get; set; } = new List<Activity>();
}
