using System;
using System.Collections.Generic;

namespace BookNumAPI.Models;

public partial class Renter
{
    public int RenterId { get; set; }

    public string RenterName { get; set; } = null!;

    public string? Gender { get; set; }

    public decimal? Height { get; set; }

    public decimal? Weight { get; set; }

    public int? Age { get; set; }

    public string? StyleTag { get; set; }

    public string? Description { get; set; }

    public int HrRate { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreateAt { get; set; }

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual ICollection<RenterPhoto> RenterPhotos { get; set; } = new List<RenterPhoto>();
}
