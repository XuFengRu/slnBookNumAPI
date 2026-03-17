using System;
using System.Collections.Generic;

namespace BookNumAPI.Models;

public partial class Account
{
    public int AccountId { get; set; }

    public string Account1 { get; set; } = null!;

    public string? Password { get; set; }

    public string? AuthProvider { get; set; }

    public string? AuthProviderId { get; set; }

    public int Role { get; set; }

    public bool? IsEmailVerified { get; set; }

    public DateTime? CreateAt { get; set; }

    public DateTime? UpdateAt { get; set; }

    public string? LastLoginIp { get; set; }

    public DateTime? LastLoginAt { get; set; }

    public int? Status { get; set; }

    public virtual ICollection<Token> Tokens { get; set; } = new List<Token>();

    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
