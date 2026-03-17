using System;
using System.Collections.Generic;

namespace BookNumAPI.Models;

public partial class Hobby
{
    public int HobbyId { get; set; }

    public string HobbyName { get; set; } = null!;

    public int HobbyCategoryId { get; set; }

    public bool? IsActived { get; set; }

    public virtual HobbyCategory HobbyCategory { get; set; } = null!;

    public virtual ICollection<HobbyList> HobbyLists { get; set; } = new List<HobbyList>();
}
