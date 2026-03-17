namespace BookNumAPI.Match.DTO
{
    public class MatchChatCreateDto
    {
        public int MatchedId { get; set; }
        public int SenderId { get; set; }
        public int ReceiverId { get; set; }
        public string Message { get; set; } = null!;

    }
}
