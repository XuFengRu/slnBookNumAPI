using System;
using System.Collections.Generic;

namespace BookNumAPI.Models;

public partial class Token
{
    public int TokenId { get; set; }

    public int AccountId { get; set; }

    public string Token1 { get; set; } = null!;

    public int TokenType { get; set; }

    public DateTime? CreateAt { get; set; }

    public DateTime ExpiryAt { get; set; }

    public DateTime? UsedAt { get; set; }

    public virtual Account Account { get; set; } = null!;
}
