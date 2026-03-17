using System;
using System.Collections.Generic;

namespace BookNumAPI.Models;

public partial class RenterPhoto
{
    public int PhotoId { get; set; }

    public int RenterId { get; set; }

    public string Url { get; set; } = null!;

    public int DisplayOrder { get; set; }

    public bool IsMain { get; set; }

    public virtual Renter Renter { get; set; } = null!;
}
