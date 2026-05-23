using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.SignalR;
using ChatRoomAPI.Services;
using System.Collections.Concurrent;
using Microsoft.AspNetCore.Authorization;
using ChatRoomAPI.Data;


namespace ChatRoomAPI.Hubs
{
    [Authorize]
    public class ChatHub(AppDbContext db) : Hub    
    {
        private readonly AppDbContext _db = db;

        // Current Memory Map of Online Users, connectionId -> roomId
        // roomId can be null, indicating the user is in the lobby
        static readonly ConcurrentDictionary<string, string?> OnlineUsers = new();

        // When a client connects to the hub
        public override async Task OnConnectedAsync()
        {
            OnlineUsers.TryAdd(Context.ConnectionId, null); 
            var userName = Context.User?.FindFirst(JwtRegisteredClaimNames.UniqueName)?.Value;
            await Clients.Caller.SendAsync("Connected", userName ?? "Unknown");
            await base.OnConnectedAsync();
        }

        // When a client disconnects from the hub
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            if (OnlineUsers.TryRemove(Context.ConnectionId, out var roomId) && roomId != null)
            {
                // User was in a room, remove them from it
                await Clients.Group(roomId).SendAsync("UserLeftRoom", Context.User?.FindFirst(JwtRegisteredClaimNames.UniqueName)?.Value ?? "Unknown");
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomId);
            }

            await base.OnDisconnectedAsync(exception);
        }
        
        // When a client joins a room
        public async Task JoinRoom(int roomId)
        {
            // Check if room exists in database
            var room = await _db.Rooms.FindAsync(roomId);
            if (room == null)
            {
                await Clients.Caller.SendAsync("RoomNotFound", roomId);
                return;
            }
            // If user is already in a room, remove them from it
            if (OnlineUsers.TryGetValue(Context.ConnectionId, out var currentRoomId) && currentRoomId != null)
            {
                await Clients.Group(currentRoomId).SendAsync("UserLeftRoom", Context.User?.FindFirst(JwtRegisteredClaimNames.UniqueName)?.Value ?? "Unknown");
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, currentRoomId);
            }
            // Update Online Users map
            OnlineUsers[Context.ConnectionId] = room.Id.ToString();
            // Add client to the room group
            await Groups.AddToGroupAsync(Context.ConnectionId, room.Id.ToString());
            await Clients.Caller.SendAsync("RoomJoined", room.Name);
            await Clients.Group(room.Id.ToString()).SendAsync("UserJoinedRoom", Context.User?.FindFirst(JwtRegisteredClaimNames.UniqueName)?.Value ?? "Unknown");
        }
        
        // When a client leaves a room
        public async Task LeaveRoom(int roomId)
        {
            // Verify User is in the room
            var room = await _db.Rooms.FindAsync(roomId);
            if (room == null)
            {
                await Clients.Caller.SendAsync("RoomNotFound");
                return;
            }
            if (!OnlineUsers.TryGetValue(Context.ConnectionId, out var userRoomId) || userRoomId != roomId.ToString())
            {
                await Clients.Caller.SendAsync("NotInRoom");
                return;
            }
            // Update Online Users map
            OnlineUsers[Context.ConnectionId] = null;
            // Remove client from the room group
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, room.Id.ToString());
            await Clients.Caller.SendAsync("RoomLeft", room.Name);
            await Clients.Group(room.Id.ToString()).SendAsync("UserLeftRoom", Context.User?.FindFirst(JwtRegisteredClaimNames.UniqueName)?.Value ?? "Unknown");
        }
        
    }
}