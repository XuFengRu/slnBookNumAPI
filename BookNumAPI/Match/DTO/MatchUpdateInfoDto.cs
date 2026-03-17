namespace BookNumAPI.Match.DTO
{
    public class MatchUpdateInfoDto
    {
        public string Nickname { get; set; }
        public string Bio { get; set; }
        public string CurrentCity { get; set; }
        public string Photo { get; set; }
        public string Job { get; set; }
        public List<MatchHobbyDto> Hobbies { get; set; } = new();

        // 偏好設定
        public int? GenderPrefer { get; set; } // 0=女生, 1=男生, null=不限
        public int? AgeMin { get; set; }
        public int? AgeMax { get; set; }
        public List<string>? Cities { get; set; } 

    }
}
