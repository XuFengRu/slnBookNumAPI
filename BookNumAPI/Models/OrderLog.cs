using System;
using System.Collections.Generic;

namespace BookNumAPI.Models;

public partial class OrderLog
{
    public int LogId { get; set; }

    public int OrderId { get; set; }

    public string OldStatus { get; set; } = null!;

    public string NewStatus { get; set; } = null!;

    public DateTime UpdateAt { get; set; }

    public virtual Order Order { get; set; } = null!;
}
