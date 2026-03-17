namespace BookNumAPI.Match.DTO
{
    public class LikedRequest
    {
        public int LikerUserId { get; set; }
        public int LikedUserId { get; set; }
        public bool IsLiked { get; set; }
        public double Score { get; set; }
    }
}
