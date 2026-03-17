using System;
using System.Collections.Generic;

namespace BookNumAPI.Models;

public partial class Liked
{
    public int LikedId { get; set; }

    public int LikerUserId { get; set; }

    public int LikedUserId { get; set; }

    public bool IsLiked { get; set; }

    public DateTime CreateAt { get; set; }

    public double Score { get; set; }

    public virtual User LikedUser { get; set; } = null!;

    public virtual User LikerUser { get; set; } = null!;
}
