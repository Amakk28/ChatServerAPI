using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using Microsoft.AspNetCore.Authorization;
using ChatRoomAPI.Data;
using ChatRoomAPI.Models;
using ChatRoomAPI.Models.DTOs;


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
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomId);
                await Clients.Group(roomId).SendAsync("UserLeftRoom", Context.User?.FindFirst(JwtRegisteredClaimNames.UniqueName)?.Value ?? "Unknown");
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
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, currentRoomId);
                await Clients.Group(currentRoomId).SendAsync("UserLeftRoom", Context.User?.FindFirst(JwtRegisteredClaimNames.UniqueName)?.Value ?? "Unknown");
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
                await Clients.Caller.SendAsync("RoomNotFound", roomId);
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

        // When a client sends a message to the hub
        public async Task SendMessage(string message)
        {
            // Verify if user is in the room
            if (!OnlineUsers.TryGetValue(Context.ConnectionId, out var roomId) || roomId == null)
            {
                await Clients.Caller.SendAsync("NotInRoom");
                return;
            }
            // Save user's message to DB and verify if room exists
            var room = await _db.Rooms.FindAsync(int.Parse(roomId));
            if (room == null)            {
                await Clients.Caller.SendAsync("RoomNotFound", roomId);
                return;
            }
            int userId;
            if (int.TryParse(Context.UserIdentifier, out var parsedUserId))
            {
                userId = parsedUserId;
            }
            else
            {
                await Clients.Caller.SendAsync("InvalidUser");
                return;
            }

            // Save message 
            var chatMessage = new Message
            {
                Content = message,
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                RoomId = int.Parse(roomId)
            };
            _db.Messages.Add(chatMessage);
            await _db.SaveChangesAsync();

            

            await Clients.Group(roomId).SendAsync("ReceiveMessage", new ChatMessageDto
            {
                Id = chatMessage.Id,
                RoomId = chatMessage.RoomId,
                Content = chatMessage.Content,
                CreatedAt = chatMessage.CreatedAt,
                SenderUsername = Context.User?.FindFirst(JwtRegisteredClaimNames.UniqueName)?.Value ?? "Unknown"
            });
        }

        // Method to get list of online users in a room
        public async Task GetOnlineUsersInRoom(int roomId)
        {
            var room = await _db.Rooms.FindAsync(roomId);
            if (room == null)
            {
                await Clients.Caller.SendAsync("RoomNotFound", roomId);
                return;
            }
            var usersInRoom = OnlineUsers
                .Where(x => x.Value == roomId.ToString())
                .Select(y => y.Key)
                .ToList();
            await Clients.Caller.SendAsync("OnlineUsersInRoom", usersInRoom);
        }
    }
}