using System;
using System.Collections.Generic;

namespace BookNumAPI.Models;

public partial class Method
{
    public int MethodId { get; set; }

    public string MethodName { get; set; } = null!;

    public int DurationDay { get; set; }

    public int Price { get; set; }

    public bool? IsActived { get; set; }

    public string? PayPalId { get; set; }

    public virtual ICollection<Premium> Premia { get; set; } = new List<Premium>();
}
