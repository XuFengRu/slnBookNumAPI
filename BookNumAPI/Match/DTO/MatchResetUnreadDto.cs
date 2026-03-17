namespace BookNumAPI.Match.DTO
{
    public class MatchResetUnreadDto
    {
        public int RoomId { get; set; }
        public int UserId { get; set; }
        public int LastReadMessageId { get; set; }

    }
}
