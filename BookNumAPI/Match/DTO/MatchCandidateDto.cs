namespace BookNumAPI.Match.DTO
{
    public class MatchCandidateDto
    {
        public int UserId { get; set; }
        public int Age { get; set; }              // 用生日計算
        public string Nickname { get; set; }      // Info 的暱稱
        public string Bio { get; set; }           // Info 的 Bio
        public string CurrentCity { get; set; }   // Info 的 CurrentCity
        public string Photo { get; set; }         // Info 的 Photo
        public string Job { get; set; }           // Info 的 Job
        public List<string> Hobbies { get; set; } // HobbyList → HobbyName
        public double Probability { get; set; }   // 算出來的分數
    }
}
