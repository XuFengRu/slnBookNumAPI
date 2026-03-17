using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace BookNumAPI.Match.Hubs
{
    public class ChatHub : Hub
    {
        public async Task JoinUser(string userId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, userId);
        }

        public async Task JoinRoom(string roomId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
        }
    }
}