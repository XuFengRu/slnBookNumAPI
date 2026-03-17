using BookNumAPI.Match.DTO;
using BookNumAPI.Match.Hubs;
using BookNumAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace BookNumAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MatchChatController : ControllerBase
    {
        private readonly BookNumApiContext _context;
        private readonly IHubContext<ChatHub> _hubContext;

        public MatchChatController(BookNumApiContext context, IHubContext<ChatHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        [HttpGet("{roomId}")]
        public async Task<IActionResult> GetChats(int roomId, int userId)
        {
            // 先查出聊天室
            var matched = await _context.Matcheds
                .Where(m => m.MatchedId == roomId && m.UnMatchAt == null) // 排除已解除配對
                .FirstOrDefaultAsync();
            if (matched == null) return NotFound();


            //if (matched.User1Id != userId && matched.User2Id != userId)
            //    return BadRequest("使用者不在此聊天室");

            // 判斷對方的 UserId
            int otherUserId = matched.User1Id == userId ? matched.User2Id : matched.User1Id;

            // 查出對方的暱稱
            var otherUserInfo = await _context.Infos
                .Where(i => i.UserId == otherUserId)
                .Select(i => new { i.Nickname,i.Photo })
                .FirstOrDefaultAsync();

            // 撈出歷史訊息
            var chats = await _context.Chats
                .Where(c => c.MatchedId == roomId)
                .OrderBy(c => c.SendAt)
                .Select(c => new MatchChatMessageDto
                {
                    ChatId = c.ChatId,
                    Photo = otherUserInfo.Photo,
                    MatchedId = c.MatchedId,
                    SenderId = c.SenderId,
                    ReceiverId = c.ReceiverId,
                    Message = c.Message,
                    SendAt = c.SendAt,
                    UnreadCount = 0,
                    OtherUserNickname = otherUserInfo.Nickname // 只帶對方暱稱
                })
                .ToListAsync();

            return Ok(chats);
        }


        [HttpPost]
        public async Task<IActionResult> SendMessage([FromBody] MatchChatCreateDto dto)
        {
            var chat = new Chat
            {
                MatchedId = dto.MatchedId,
                SenderId = dto.SenderId,
                ReceiverId = dto.ReceiverId,
                Message = dto.Message,
                SendAt = DateTime.Now
            };

            _context.Chats.Add(chat);
            await _context.SaveChangesAsync();

            // 計算接收者的未讀數量
            int unreadForReceiver = await GetUnreadCountAsync(chat.MatchedId, chat.ReceiverId);

            // 推播訊息 + 未讀數量
            var resultDto = new MatchChatMessageDto
            {
                ChatId = chat.ChatId,
                MatchedId = chat.MatchedId,
                SenderId = chat.SenderId,
                ReceiverId = chat.ReceiverId,
                Message = chat.Message,
                SendAt = chat.SendAt,
                UnreadCount = unreadForReceiver
            };

            await _hubContext.Clients.Group(chat.MatchedId.ToString())
                .SendAsync("ReceiveMessage", resultDto);

            //await _hubContext.Clients.Group(chat.ReceiverId.ToString())
            //    .SendAsync("ReceiveMessage", resultDto);

            //await _hubContext.Clients.Group(chat.SenderId.ToString())
            //    .SendAsync("ReceiveMessage", resultDto);


            return Ok(resultDto);
        }


        [HttpGet("list/{userId}")]
        public async Task<IActionResult> GetChatList(int userId)
        {
            var matchedRooms = await _context.Matcheds
                .Where(m => (m.User1Id == userId || m.User2Id == userId) && m.UnMatchAt == null)
                .ToListAsync();

            var roomIds = matchedRooms.Select(m => m.MatchedId).ToList();

            // 查出所有最後訊息
            var lastMessages = await _context.Chats
                .Where(c => roomIds.Contains(c.MatchedId))
                .GroupBy(c => c.MatchedId)
                .Select(g => g.OrderByDescending(c => c.SendAt).FirstOrDefault())
                .ToListAsync();

            // 查出所有對方資訊
            var otherUserIds = matchedRooms
                .Select(m => m.User1Id == userId ? m.User2Id : m.User1Id)
                .ToList();

            var infos = await _context.Infos
                .Where(i => otherUserIds.Contains(i.UserId))
                .ToListAsync();

            var result = new List<MatchChatListDto>();

            foreach (var room in matchedRooms)
            {
                int otherUserId = room.User1Id == userId ? room.User2Id : room.User1Id;
                var otherUserInfo = infos.FirstOrDefault(i => i.UserId == otherUserId);

                var lastMessage = lastMessages.FirstOrDefault(c => c?.MatchedId == room.MatchedId);

                // ✅ 使用 LastReadMessageId 判斷未讀數
                int? lastReadMessageId = null;
                if (userId == room.User1Id)
                    lastReadMessageId = room.User1LastReadMessageId;
                else if (userId == room.User2Id)
                    lastReadMessageId = room.User2LastReadMessageId;

                int unreadCount = await _context.Chats
                    .Where(c => c.MatchedId == room.MatchedId
                             && c.ReceiverId == userId
                             && (lastReadMessageId == null || c.ChatId > lastReadMessageId))
                    .CountAsync();

                result.Add(new MatchChatListDto
                {
                    RoomId = room.MatchedId,
                    OtherUserId = otherUserId,
                    OtherUserNickname = otherUserInfo?.Nickname,
                    OtherUserPhoto = otherUserInfo?.Photo,
                    LastMessage = lastMessage?.Message,
                    LastTime = lastMessage?.SendAt ?? room.CreateAt,
                    UnreadCount = unreadCount
                });
            }

            return Ok(result.OrderByDescending(r => r.LastTime).ToList());
        }


        // 前端可呼叫此 API 重置未讀（紅點）
        [HttpPost("read/reset")]
        public async Task<IActionResult> ResetUnreadCount([FromBody] MatchResetUnreadDto dto)
        {
            var matched = await _context.Matcheds.FindAsync(dto.RoomId);
            if (matched == null)
                return NotFound();

            if (matched.User1Id != dto.UserId && matched.User2Id != dto.UserId)
                return BadRequest("使用者不在此聊天室");

            // 更新已讀訊息 ID
            await MarkAsReadAsync(dto.RoomId, dto.UserId, dto.LastReadMessageId);

            // 重新計算未讀數量
            int unreadCount = await GetUnreadCountAsync(dto.RoomId, dto.UserId);

            // 推播通知前端更新未讀數量
            await _hubContext.Clients.Group(dto.RoomId.ToString())
                .SendAsync("ResetUnread", new
                {
                    RoomId = dto.RoomId,
                    UserId = dto.UserId,
                    UnreadCount = unreadCount
                });

            return Ok();
        }

        [HttpPost("unmatch")]
        public async Task<IActionResult> UnMatch([FromBody] MatchUnMatchDto dto)
        {
            var matched = await _context.Matcheds.FindAsync(dto.RoomId);
            if (matched == null) return NotFound();

            if (matched.User1Id != dto.UserId && matched.User2Id != dto.UserId)
                return BadRequest("使用者不在此聊天室");

            // 存解除配對時間
            matched.UnMatchAt = DateTime.Now;
            await _context.SaveChangesAsync();

            // 推播通知前端 (可選)
            await _hubContext.Clients.Group(dto.RoomId.ToString())
                .SendAsync("UnMatched", new
                {
                    RoomId = dto.RoomId,
                    UserId = dto.UserId,
                    UnMatchAt = matched.UnMatchAt
                });

            return Ok(new { Success = true, RoomId = dto.RoomId, UnMatchAt = matched.UnMatchAt });

        }

        // 更新使用者的最後已讀訊息 ID
        private async Task MarkAsReadAsync(int matchedId, int userId, int lastReadMessageId)
        {
            var matched = await _context.Matcheds.FindAsync(matchedId);
            if (matched == null) return;

            if (userId == matched.User1Id)
            {
                matched.User1LastReadMessageId = Math.Max(matched.User1LastReadMessageId ?? 0, lastReadMessageId);
            }
            else if (userId == matched.User2Id)
            {
                matched.User2LastReadMessageId = Math.Max(matched.User2LastReadMessageId ?? 0, lastReadMessageId);
            }

            await _context.SaveChangesAsync();
        }

        private async Task<int> GetUnreadCountAsync(int matchedId, int userId)
        {
            var matched = await _context.Matcheds.FindAsync(matchedId);
            if (matched == null) return 0;

            int? lastReadMessageId = null;

            if (userId == matched.User1Id)
                lastReadMessageId = matched.User1LastReadMessageId;
            else if (userId == matched.User2Id)
                lastReadMessageId = matched.User2LastReadMessageId;

            // 如果還沒讀過任何訊息，全部算未讀
            var unreadCount = await _context.Chats
                .Where(c => c.MatchedId == matchedId
                         && c.ReceiverId == userId
                         && (lastReadMessageId == null || c.ChatId > lastReadMessageId))
                .CountAsync();

            return unreadCount;
        }
    }
}

