using BookNumAPI.Match.Hubs;
using BookNumAPI.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace BookNumAPI.Match.Events.Handlers
{
    public class ChatRoomCreatedHandler : IEventHandler<MatchChatRoomCreatedEvent>
    {
        private readonly BookNumApiContext _context;
        private readonly IHubContext<ChatHub> _hubContext;

        public ChatRoomCreatedHandler(BookNumApiContext context, IHubContext<ChatHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        public async Task Handle(MatchChatRoomCreatedEvent @event)
        {
            var matched = await _context.Matcheds
                .Include(m => m.User1).ThenInclude(u => u.Infos)
                .Include(m => m.User2).ThenInclude(u => u.Infos)
                .FirstOrDefaultAsync(m => m.MatchedId == @event.RoomId);

            if (matched == null) return;

            // 取得 User1 的最新 Info
            var user1Info = matched.User1.Infos?.OrderByDescending(i => i.InfoId).FirstOrDefault();
            // 取得 User2 的最新 Info
            var user2Info = matched.User2.Infos?.OrderByDescending(i => i.InfoId).FirstOrDefault();

            // 推播通知 User1 有新聊天室建立
            await _hubContext.Clients.Groups(
                matched.User1Id.ToString()
            ).SendAsync("ChatRoomCreated", new
            {
                RoomId = matched.MatchedId,
                OtherUserId = matched.User2Id,
                OtherUserNickname = user2Info?.Nickname,
                OtherUserPhoto = user2Info?.Photo
            });

            // 推播通知 User2 有新聊天室建立
            await _hubContext.Clients.Groups(
                matched.User2Id.ToString()
            ).SendAsync("ChatRoomCreated", new
            {
                RoomId = matched.MatchedId,
                OtherUserId = matched.User1Id,
                OtherUserNickname = user1Info?.Nickname,
                OtherUserPhoto = user1Info?.Photo
            });
        }
    }
}