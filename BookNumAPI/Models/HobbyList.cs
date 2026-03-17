using System;
using System.Collections.Generic;

namespace BookNumAPI.Models;

public partial class HobbyList
{
    public int HobbyListId { get; set; }

    public int UserId { get; set; }

    public int HobbyId { get; set; }

    public virtual Hobby Hobby { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
