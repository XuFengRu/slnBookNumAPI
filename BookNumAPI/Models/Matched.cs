using System;
using System.Collections.Generic;

namespace BookNumAPI.Models;

public partial class Matched
{
    public int MatchedId { get; set; }

    public int User1Id { get; set; }

    public int User2Id { get; set; }

    public DateTime CreateAt { get; set; }

    public DateTime? UnMatchAt { get; set; }

    public double Score { get; set; }

    public int? User1LastReadMessageId { get; set; }

    public int? User2LastReadMessageId { get; set; }

    public virtual ICollection<Chat> Chats { get; set; } = new List<Chat>();

    public virtual User User1 { get; set; } = null!;

    public virtual User User2 { get; set; } = null!;
}
