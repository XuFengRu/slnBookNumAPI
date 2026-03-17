namespace BookNumAPI.Match.Events
{
    public class MatchChatRoomCreatedEvent
    {
        public int RoomId { get; set; }
        public int User1Id { get; set; }
        public int User2Id { get; set; }
    }
}
