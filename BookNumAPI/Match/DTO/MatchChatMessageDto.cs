namespace BookNumAPI.Match.DTO
{
    public class MatchChatMessageDto
    {

        public int ChatId { get; set; }
        public string? Photo { get; set; }
        public int MatchedId { get; set; }
        public int SenderId { get; set; }
        public int ReceiverId { get; set; }
        public string Message { get; set; } = null!;
        public DateTime SendAt { get; set; }
        public int UnreadCount { get; set; }
        public string? OtherUserNickname { get; set; }
    }
}
