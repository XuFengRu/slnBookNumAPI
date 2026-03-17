using System;
using System.Collections.Generic;

namespace BookNumAPI.Models;

public partial class Activity
{
    public int ActivityId { get; set; }

    public int UserId { get; set; }

    public int CategoryId { get; set; }

    public string Title { get; set; } = null!;

    public string Description { get; set; } = null!;

    public DateTime EventDate { get; set; }

    public DateTime StartAt { get; set; }

    public DateTime EndAt { get; set; }

    public int MaxPeople { get; set; }

    public string Location { get; set; } = null!;

    public int Status { get; set; }

    public DateTime CreateAt { get; set; }

    public string? Image { get; set; }

    public virtual ActivityCategory Category { get; set; } = null!;

    public virtual ICollection<Participant> Participants { get; set; } = new List<Participant>();

    public virtual User User { get; set; } = null!;
}
