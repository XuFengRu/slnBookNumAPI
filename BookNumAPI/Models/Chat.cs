using System;
using System.Collections.Generic;

namespace BookNumAPI.Models;

public partial class Chat
{
    public int ChatId { get; set; }

    public int SenderId { get; set; }

    public int ReceiverId { get; set; }

    public string Message { get; set; } = null!;

    public DateTime SendAt { get; set; }

    public int MatchedId { get; set; }

    public virtual Matched Matched { get; set; } = null!;

    public virtual User Receiver { get; set; } = null!;

    public virtual User Sender { get; set; } = null!;
}
