using System;
using System.Collections.Generic;

namespace BookNumAPI.Models;

public partial class Info
{
    public int InfoId { get; set; }

    public int UserId { get; set; }

    public string? Nickname { get; set; }

    public string? Photo { get; set; }

    public string? Bio { get; set; }

    public decimal? Height { get; set; }

    public decimal? Weight { get; set; }

    public int? AgeMax { get; set; }

    public int? AgeMin { get; set; }

    public bool? GenderPrefer { get; set; }

    public string? CurrentCity { get; set; }

    public decimal? Latitude { get; set; }

    public decimal? Longitude { get; set; }

    public string? Job { get; set; }

    public virtual User User { get; set; } = null!;
}
