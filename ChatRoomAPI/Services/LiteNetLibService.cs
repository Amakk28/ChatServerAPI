// using System;
// using System.Collections.Generic;
// using System.Linq;
// using System.Threading.Tasks;
// using Microsoft.Extensions.Hosting;
// using LiteNetLib;
// using ChatRoomAPI.Data;
// using ChatRoomAPI.Models.DTOs;
// using System.IdentityModel.Tokens.Jwt;
// using ChatRoomAPI.Models;
// using Microsoft.IdentityModel.Tokens;
// using System.Text;
// using System.Text.Json;
// using Microsoft.EntityFrameworkCore;
// using LiteNetLib.Utils;

// namespace ChatRoomAPI.Services
// {
//     public class LiteNetLibService(IServiceScopeFactory serviceScopeFactory, ILogger<LiteNetLibService> logger, IConfiguration configuration) : BackgroundService
//     {
//         const int PORT = 9050;
//         private EventBasedNetListener _listener = new();
//         private NetManager? _server;
        
//         private enum Header : byte
//         {
//             Auth = 0,
//             Join = 1,
//             Sync = 2,
//         }
//         private async Task JoinRoom(NetPeer peer, NetPacketReader reader)
//         {
//             logger.LogDebug("Joining Room...");
//             var userConnection = GameStateCache.ConnectedClients
//                 .FirstOrDefault(connection => ReferenceEquals(connection.Value, peer));
//             if (userConnection.Equals(default(KeyValuePair<int, NetPeer>)))
//             {
//                 logger.LogWarning("Unauthenticated peer attempted to join a room.");
//                 peer.Disconnect();
//                 return;
//             }
//             // Check cache first for a gamestate, that means another player joined that room and loaded it into cache
//             // If you do not, you will get an outdated gamestate from db
//             // You check db afterwards if gamestate for said room does not exist, it ensures that 
//             // the game state you are getting is actually up to date as a gamestate removed from cache will
//             // also save that gamestate to db
//             var userId = userConnection.Key;
//             var roomId = reader.GetInt();
//             if (!GameStateCache.GameStates.TryGetValue(roomId, out var gameState))
//             {
//                 using var scope = serviceScopeFactory.CreateScope();
//                 var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
//                 var dbGameState = await db.GameStates
//                     .Include(state => state.Units)
//                     .FirstOrDefaultAsync(state => state.RoomId == roomId);
//                 if (dbGameState == null)
//                 {
//                     logger.LogWarning("User {UserId} attempted to join nonexistent room {RoomId}.", userId, roomId);
//                     return;
//                 }

//                 gameState = new GameStateDto
//                 {
//                     RoomId = dbGameState.RoomId,
//                     OwnerId = dbGameState.OwnerId,
//                     CurrentTurnPlayerId = dbGameState.CurrentTurnPlayerId,
//                     TurnNumber = dbGameState.TurnNumber,
//                     Units = dbGameState.Units.Select(unit => new UnitDto
//                     {
//                         Name = unit.Name,
//                         Type = unit.Type,
//                         OwnerPlayerId = unit.OwnerPlayerId,
//                         GameStateId = unit.GameStateId,
//                         X = unit.X,
//                         Y = unit.Y,
//                         Z = unit.Z,
//                         RotationY = unit.RotationY,
//                         Health = unit.Health,
//                         HasMoved = unit.HasMoved
//                     }).ToList()
//                 };

//                 gameState = GameStateCache.GameStates.GetOrAdd(roomId, gameState);
//             }

//             GameStateCache.UserToRoom.AddOrUpdate(userId, roomId, (_, _) => roomId);

//             var writer = new NetDataWriter();
//             writer.Put((byte)Header.Join);
//             writer.Put(JsonSerializer.Serialize(gameState));
//             peer.Send(writer, DeliveryMethod.ReliableOrdered);
//         }
//         private async void Authenticator(NetPeer peer, string token)
//         {
//             try
//             {
//                 var handler = new JwtSecurityTokenHandler()
//                 {
//                     MapInboundClaims = false
//                 };
//                 var validationParameters = new TokenValidationParameters
//                 {
//                     ValidateIssuerSigningKey = true,
//                     IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Environment.GetEnvironmentVariable("JWT_KEY") ?? configuration["Jwt:Key"]!)),
//                     ValidateIssuer = true,
//                     ValidIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? configuration["Jwt:Issuer"],
//                     ValidateAudience = true,
//                     ValidAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? configuration["Jwt:Audience"],
//                     ValidateLifetime = true, // rejects expired tokens
//                     ClockSkew = TimeSpan.Zero
//                 };

//                 var principal = handler.ValidateToken(token, validationParameters, out _);
//                 var userIdClaim = principal.FindFirst(JwtRegisteredClaimNames.Sub);
                
//                 if (userIdClaim == null) // If for some reason the user did not have a sub claim (userId)
//                 {
//                     peer.Disconnect();
//                     logger.LogError("A user's claim is incorrect when validating!");
//                     return;
//                 }

//                 int userId = int.Parse(userIdClaim.Value);

//                 using var scope = serviceScopeFactory.CreateScope();
//                 var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

//                 var user = await db.Users.FindAsync(userId);
//                 if (user == null) // If user was not found in database
//                 {
//                     logger.LogError("A user was not found in the database!");
//                     peer.Disconnect();
//                     return;
//                 }
//                 // Cache user to connected clients
//                 if (!GameStateCache.ConnectedClients.TryAdd(userId, peer))
//                 {
//                     logger.LogError("User is already connected!");
//                     peer.Disconnect();
//                 }
//             }
//             catch (Exception exception)
//             {
//                 logger.LogError(exception, "User failed authentication!");
//                 peer.Disconnect();
//             }
//         }
//         // Handle Sync data from client
//         private static void HandleSync(NetPeer peer, NetPacketReader reader) 
//         {
//             // Validate user briefly
//             var userId = GameStateCache.ConnectedClients.FirstOrDefault(x => ReferenceEquals(x.Value, peer)).Key;
//             if (userId == 0)
//             {
//                 peer.Disconnect();
//                 return;
//             }
//             // Check if user is actually in room
//             if (!GameStateCache.UserToRoom.TryGetValue(userId, out var roomId))
//             {
//                 peer.Disconnect();
//                 return;
//             }
//             // Read data, has to be implemented
//         }

//         protected override async Task ExecuteAsync(CancellationToken stoppingToken)
//         {
//             _server = new NetManager(_listener)
//             {
//                 AutoRecycle = true
//             };
//             _server.Start(PORT);
//             logger.LogInformation("LiteNetLib server started on port 9050");
            
//             // Handler for incoming connection requests
//             _listener.ConnectionRequestEvent += request =>
//             {
//                 if (logger.IsEnabled(LogLevel.Debug))
//                     logger.LogDebug("Connection request from {RemoteEndPoint}, data: {Data}", request.RemoteEndPoint, request.Data);
//                 request.AcceptIfKey("GameKey");
//             };
//             // Handler for connected peers
//             _listener.PeerConnectedEvent += peer =>
//             {
//                 if (logger.IsEnabled(LogLevel.Debug))
//                     logger.LogDebug("Client connected: {Address}:{Port}, ID: {Id}, Ping: {Ping}", peer.Address, peer.Port, peer.Id, peer.Ping);
//             };
//             // Handler for when a client sends data to the server
//             // Peer is the client
//             // Reader is used to read the data sent by the client
//             // Channel is the channel the data was sent on
//             // Method is whether the data was sent reliably or unreliably
//             _listener.NetworkReceiveEvent += async (peer, reader, channel, method) =>
//             {
//                 var header = reader.GetByte();
//                 if (logger.IsEnabled(LogLevel.Debug))
//                 {
//                     var cacheState = new
//                     {
//                         OnlineUsers = GameStateCache.UserToRoom,
//                         GameStates = GameStateCache.GameStates,
//                         ConnectedClients = GameStateCache.ConnectedClients.ToDictionary(
//                             connection => connection.Key,
//                             connection => new
//                             {
//                                 PeerId = connection.Value.Id,
//                                 Address = connection.Value.Address.ToString(),
//                                 Port = connection.Value.Port
//                             })
//                     };
//                     logger.LogDebug("Cache state on network receive: {CacheState}", JsonSerializer.Serialize(cacheState));
//                 }
//                 if (logger.IsEnabled(LogLevel.Debug))
//                     logger.LogDebug("Client ID: {Id} sent: {Header}, channel: {Channel}, method: {Method}", peer.Id, header, channel, method);

//                 switch (header)
//                 {
//                     case 0:
//                         Authenticator(peer, reader.GetString());
//                         break;
//                     case 1:
//                         _ = JoinRoom(peer, reader);
//                         break;
//                     default:
//                         HandleSync(peer, reader);
//                         break;
//                 }
//             };
//             // Handler for when client disconnects
//             _listener.PeerDisconnectedEvent += (peer, disconnectInfo) =>
//             {
//                 if (logger.IsEnabled(LogLevel.Debug))
//                     logger.LogDebug("Client ID: {Id} disconnected, {Reason}", peer.Id, disconnectInfo.Reason);
//                 foreach (var connection in GameStateCache.ConnectedClients)
//                 {
//                     // Try to remove from connected clients cache dict
//                     if (ReferenceEquals(connection.Value, peer) && GameStateCache.ConnectedClients.TryRemove(connection.Key, out _))
//                     {
//                         // Also try to remove from user to roomId cache
//                         GameStateCache.UserToRoom.TryRemove(connection.Key, out var roomId);
//                         if (logger.IsEnabled(LogLevel.Debug))
//                             logger.LogDebug("User {UserId} disconnected, from room: {RoomId}, for: {Reason}", connection.Key, roomId, disconnectInfo.Reason);
//                         break;
//                     }
//                 }
//             };

//             while (!stoppingToken.IsCancellationRequested)
//             {
//                 _server.PollEvents();
//                 await Task.Delay(10, stoppingToken);
//             }
//         }
//     }
// }