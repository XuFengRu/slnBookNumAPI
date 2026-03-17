using System;
using System.Collections.Generic;

namespace BookNumAPI.Models;

public partial class Participant
{
    public int ParticipantId { get; set; }

    public int ActivityId { get; set; }

    public int UserId { get; set; }

    public int JoinStatus { get; set; }

    public DateTime CreateAt { get; set; }

    public DateTime? UpdateAt { get; set; }

    public virtual Activity Activity { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
