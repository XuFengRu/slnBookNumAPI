using System;
using System.Collections.Generic;

namespace BookNumAPI.Models;

public partial class HobbyCategory
{
    public int HobbyCategoryId { get; set; }

    public string CategoryName { get; set; } = null!;

    public virtual ICollection<Hobby> Hobbies { get; set; } = new List<Hobby>();
}
