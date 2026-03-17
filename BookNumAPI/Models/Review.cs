using System;
using System.Collections.Generic;

namespace BookNumAPI.Models;

public partial class Review
{
    public int ReviewId { get; set; }

    public int OrderId { get; set; }

    public int Rating { get; set; }

    public string? Comment { get; set; }

    public DateTime CreateAt { get; set; }

    public virtual Order Order { get; set; } = null!;
}
