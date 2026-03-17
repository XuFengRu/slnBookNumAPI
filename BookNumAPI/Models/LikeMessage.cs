using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookNumAPI.Models;

public partial class LikeMessage
{
    public int LikeMessageId { get; set; }

    public int SenderId { get; set; }

    public int ReceiverId { get; set; }

    public string Message { get; set; } = null!;

    [InverseProperty("LikeMessageReceivers")]
    public virtual User Receiver { get; set; } = null!;

    [InverseProperty("LikeMessageSenders")]
    public virtual User Sender { get; set; } = null!;
}
