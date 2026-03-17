namespace BookNumAPI.Match.DTO
{
    public class MatchChatListDto
    {
        public int RoomId { get; set; }
        public int OtherUserId { get; set; }
        public string? OtherUserNickname { get; set; }
        public string? OtherUserPhoto { get; set; }
        public string? LastMessage { get; set; }
        public DateTime? LastTime { get; set; }
        public int UnreadCount { get; set; }

    }
}
