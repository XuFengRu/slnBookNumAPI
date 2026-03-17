namespace BookNumAPI.Match.DTO
{
    public class MatchInfoDto
    {
        public int UserId { get; set; }
        public string Nickname { get; set; }
        public int Age { get; set; }
        public string Bio { get; set; }
        public string CurrentCity { get; set; }
        public string Photo { get; set; }
        public string Job { get; set; }
        public List<MatchHobbyDto> Hobbies { get; set; } = new();
        public PreferenceDto Preferences { get; set; }
        public StatsDto Stats { get; set; }
    }

    public class PreferenceDto
    {
        public string Gender { get; set; }
        public int? AgeMin { get; set; }
        public int? AgeMax { get; set; }
        public List<string> Cities { get; set; }
    }

    public class StatsDto
    {
        public int Matches { get; set; }
        public int Likes { get; set; }
    }
}
