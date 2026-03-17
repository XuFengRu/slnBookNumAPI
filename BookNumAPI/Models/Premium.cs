using System;
using System.Collections.Generic;

namespace BookNumAPI.Models;

public partial class Premium
{
    public int PremiumId { get; set; }

    public int UserId { get; set; }

    public DateTime StartAt { get; set; }

    public DateTime EndAt { get; set; }

    public int MethodId { get; set; }

    public int Price { get; set; }

    public bool AutoRenew { get; set; }

    public string SubscriptionId { get; set; } = null!;

    public virtual Method Method { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
