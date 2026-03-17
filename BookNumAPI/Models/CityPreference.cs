using System;
using System.Collections.Generic;

namespace BookNumAPI.Models;

public partial class CityPreference
{
    public int CityPreferencesId { get; set; }

    public int UserId { get; set; }

    public string City { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
