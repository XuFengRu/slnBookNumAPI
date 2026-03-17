using System;
using System.Collections.Generic;

namespace BookNumAPI.Models;

public partial class User
{
    public int UserId { get; set; }

    public int AccountId { get; set; }

    public string? Name { get; set; }

    public string? Gender { get; set; }

    public DateOnly? Birthdate { get; set; }

    public string? Phone { get; set; }

    public virtual Account Account { get; set; } = null!;

    public virtual ICollection<Activity> Activities { get; set; } = new List<Activity>();

    public virtual ICollection<Chat> ChatReceivers { get; set; } = new List<Chat>();

    public virtual ICollection<Chat> ChatSenders { get; set; } = new List<Chat>();

    public virtual ICollection<CityPreference> CityPreferences { get; set; } = new List<CityPreference>();

    public virtual ICollection<HobbyList> HobbyLists { get; set; } = new List<HobbyList>();

    public virtual ICollection<Info> Infos { get; set; } = new List<Info>();

    public virtual ICollection<Liked> LikedLikedUsers { get; set; } = new List<Liked>();

    public virtual ICollection<Liked> LikedLikerUsers { get; set; } = new List<Liked>();

    public virtual ICollection<Matched> MatchedUser1s { get; set; } = new List<Matched>();

    public virtual ICollection<Matched> MatchedUser2s { get; set; } = new List<Matched>();

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual ICollection<Participant> Participants { get; set; } = new List<Participant>();

    public virtual ICollection<Premium> Premia { get; set; } = new List<Premium>();
}
