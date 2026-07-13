using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using Microsoft.AspNetCore.Authorization;
using ChatRoomAPI.Data;
using ChatRoomAPI.Models;
using ChatRoomAPI.Models.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;
using System.Runtime.InteropServices;


namespace ChatRoomAPI.Hubs
{
    [Authorize]
    public class ChatHub(AppDbContext db) : Hub    
    {
        private readonly AppDbContext _db = db;

        // Current Memory Map of Online Users, connectionId -> roomId
        // roomId can be null, indicating the user is in the lobby
        static readonly ConcurrentDictionary<string, string?> OnlineUsers = new();

        // Map of game states for each room, roomId -> GameState
        static readonly ConcurrentDictionary<int, GameStateDto> GameStates = new();
        // Map of to derive user id easily, connectionId -> userId

        // When a client connects to the hub
        public override async Task OnConnectedAsync()
        {
            // Create User DTO
            var user = await _db.Users.FindAsync(int.Parse(Context.UserIdentifier ?? "0"));
            UserDto userDto = UserDto.FromUser(user!);
            
            OnlineUsers.TryAdd(Context.ConnectionId, null); 
            await Clients.Caller.SendAsync("Connected", userDto);
            await base.OnConnectedAsync();
        }

        // When a client disconnects from the hub
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            if (OnlineUsers.TryRemove(Context.ConnectionId, out var roomId) && roomId != null)
            {
                // A disconnected user must have their data saved to the database, and they player data removed from cache
                if (GameStates.TryGetValue(int.Parse(roomId), out GameStateDto? gameState))
                {
                    // Free memory in cache, the whole game state if the user is the owner, or just the specific unit otherwise
                    if (gameState.OwnerId == int.Parse(Context.UserIdentifier!))
                    {
                        if (GameStates.TryRemove(int.Parse(roomId), out gameState))
                        {
                            Console.WriteLine("Host User Disconnected, Cache data freed");
                        }
                    }  
                    else
                    {
                        // Find unit belonging to user that disconnected, and remove it from cache
                        UnitDto? unit = gameState.Units.Find(u => u.OwnerPlayerId == int.Parse(Context.UserIdentifier!));
                        if (unit != null) 
                        {
                            gameState.Units.Remove(unit);
                            Console.WriteLine("User Disconnected, Cache data freed");
                        }    
                    }
                // CONTINUE WORKING                     
                }
                // User was in a room, remove them from it
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomId);
                await Clients.Group(roomId).SendAsync("UserDisconnected", Context.User?.FindFirst(JwtRegisteredClaimNames.UniqueName)?.Value ?? "Unknown");
            }

            await base.OnDisconnectedAsync(exception);
        }

        // Take in Unit data from client, make a unit and update the database and cache it.
        public async Task CreateUnit(UnitDto unitDto)
        {
            var gameState = await _db.GameStates.FindAsync(unitDto.GameStateId);
            if (gameState == null)
            {
                await Clients.Caller.SendAsync("GameStateNotFound", unitDto.GameStateId);
                return;
            }

            var unit = new Unit
            {
                OwnerPlayerId = unitDto.OwnerPlayerId,
                X = unitDto.X,
                Y = unitDto.Y,
                Health = unitDto.Health,
                HasMoved = unitDto.HasMoved,
                GameStateId = unitDto.GameStateId,
                GameState = gameState
            };

            _db.Units.Add(unit);
            await _db.SaveChangesAsync();
            // Cache it as well
            GameStates.TryGetValue(gameState.RoomId, out GameStateDto? gameStateDto);
            if (gameStateDto == null)
            {
                await Clients.Caller.SendAsync("FatalSync");
                return;
            }
            gameStateDto.Units.Add(unitDto);
            // Broadcast
            await Clients.OthersInGroup(gameState.RoomId.ToString()).SendAsync("NewUserJoined", gameStateDto, unitDto.OwnerPlayerId);
            await Clients.Group(gameState.RoomId.ToString()).SendAsync("SyncGameState", gameStateDto);
            
        }


        // Sync game state in memory with database, like a save in memory
        public async Task SyncGameState(GameStateDto gameState)
        {
            // Console.WriteLine($"Server received position: {gameState.Units[0].X}, {gameState.Units[0].Y}");
            if (gameState == null)
            {
                await Clients.Caller.SendAsync("GameStateNotFound", gameState?.RoomId);
                return;
            }
            GameStates.AddOrUpdate(gameState.RoomId, gameState, (key, old) => gameState);
            await Clients.Group(gameState.RoomId.ToString()).SendAsync("SyncGameState", gameState);
        }

        // When a client joins a room
        public async Task JoinRoom(int roomId)
        {
            var user = await _db.Users.FindAsync(int.Parse(Context.UserIdentifier ?? "0"));
            if (user == null)
            {
                await Clients.Caller.SendAsync("UserNotFound");
                return;
            }

            var userDto = UserDto.FromUser(user);
            var room = await _db.Rooms.FindAsync(roomId);
            if (room == null)
            {
                await Clients.Caller.SendAsync("RoomNotFound", roomId);
                return;
            }

            if (OnlineUsers.TryGetValue(Context.ConnectionId, out var currentRoomId) && currentRoomId != null)
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, currentRoomId);
                await Clients.Group(currentRoomId).SendAsync("UserLeftRoom", user.Username);
            }

            OnlineUsers[Context.ConnectionId] = room.Id.ToString();
            await Groups.AddToGroupAsync(Context.ConnectionId, room.Id.ToString());

            // Check cache first, only read from DB if not in cache
            if (!GameStates.TryGetValue(room.Id, out GameStateDto? gameStateDto) || gameStateDto == null)
            {
                var gameState = await _db.GameStates
                    .Include(gs => gs.Units)
                    .FirstOrDefaultAsync(gs => gs.RoomId == room.Id);

                if (gameState == null)
                {
                    await Clients.Caller.SendAsync("GameStateNotFound", room.Id);
                    return;
                }

                gameStateDto = new GameStateDto
                {
                    OwnerId = gameState.OwnerId,
                    RoomId = gameState.RoomId,
                    CurrentTurnPlayerId = gameState.CurrentTurnPlayerId,
                    TurnNumber = gameState.TurnNumber,
                    Units = gameState.Units.Select(u => new UnitDto
                    {
                        OwnerPlayerId = u.OwnerPlayerId,
                        GameStateId = u.GameStateId,
                        X = u.X,
                        Y = u.Y,
                        Health = u.Health,
                        HasMoved = u.HasMoved
                    }).ToList()
                };

                // Only add if another player hasn't added it yet 
                GameStates.TryAdd(room.Id, gameStateDto);
                // Get the actual cached version in case another player joined the room
                GameStates.TryGetValue(room.Id, out gameStateDto);
            }

            await Clients.Caller.SendAsync("SyncGameState", gameStateDto);
            await Clients.Group(room.Id.ToString()).SendAsync("UserJoinedRoom", userDto);
        }
        
        // When a client leaves a room
        public async Task LeaveRoom(int roomId)
        {
            // Create User DTO
            var user = await _db.Users.FindAsync(int.Parse(Context.UserIdentifier ?? "0"));
            UserDto userDto = UserDto.FromUser(user!);
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
            await Clients.Group(room.Id.ToString()).SendAsync("UserDisconnected", userDto);
        }

        // When a client sends a message to the hub
        public async Task SendMessage(string message)
        {
            var user = await _db.Users.FindAsync(int.Parse(Context.UserIdentifier ?? "0"));
            // Verify if user is in the room
            if (!OnlineUsers.TryGetValue(Context.ConnectionId, out var roomId) || roomId == null)
            {
                await Clients.Caller.SendAsync("NotInRoom");
                return;
            }
            // Send message to group directly
            await Clients.Group(roomId).SendAsync("ReceiveMessage", new ChatMessageDto
            {
                Content = message,
                CreatedAt = DateTime.UtcNow,
                SenderUsername = user!.Username,
                TextColor = user.TextColor
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
        
        // Method to update the text color of a user
        public async Task UpdateTextColor(string newColor)
        {
            var user = await _db.Users.FindAsync(int.Parse(Context.UserIdentifier ?? "0"));
            if (user == null)
            {
                await Clients.Caller.SendAsync("UserNotFound");
                return;
            }
            user.TextColor = newColor;
            await _db.SaveChangesAsync();
            await Clients.Caller.SendAsync("TextColorUpdated", newColor);
        }

        public static Dictionary<string, int> GetOnlineUserCounts()
        {
            return OnlineUsers.Values
                .Where(roomId => roomId != null)
                .GroupBy(roomId => roomId)
                .ToDictionary(g => g.Key!, g => g.Count());
        }
    }
}