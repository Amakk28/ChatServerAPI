using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using LiteNetLib;
using ChatRoomAPI.Data;
using ChatRoomAPI.Models.DTOs;
using System.IdentityModel.Tokens.Jwt;
using ChatRoomAPI.Models;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace ChatRoomAPI.Services
{
    public class LiteNetLibService(IServiceScopeFactory serviceScopeFactory, ILogger<LiteNetLibService> logger, IConfiguration configuration) : BackgroundService
    {
        const int PORT = 9050;
        private EventBasedNetListener _listener = new();
        private NetManager? _server;
        
        private enum Header : byte
        {
            Auth = 0, 

        }
        private async void Authenticator(NetPeer peer, string token)
        {
            try
            {
                var handler = new JwtSecurityTokenHandler()
                {
                    MapInboundClaims = false
                };
                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Environment.GetEnvironmentVariable("JWT_KEY") ?? configuration["Jwt:Key"]!)),
                    ValidateIssuer = true,
                    ValidIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? configuration["Jwt:Issuer"],
                    ValidateAudience = true,
                    ValidAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? configuration["Jwt:Audience"],
                    ValidateLifetime = true, // rejects expired tokens
                    ClockSkew = TimeSpan.Zero
                };

                var principal = handler.ValidateToken(token, validationParameters, out _);
                var userIdClaim = principal.FindFirst(JwtRegisteredClaimNames.Sub);
                
                if (userIdClaim == null) // If for some reason the user did not have a sub claim (userId)
                {
                    peer.Disconnect();
                    logger.LogError("A user's claim is incorrect when validating!");
                    return;
                }

                int userId = int.Parse(userIdClaim.Value);

                using var scope = serviceScopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var user = await db.Users.FindAsync(userId);
                if (user == null) // If user was not found in database
                {
                    peer.Disconnect();
                    logger.LogError("A user was not found in the database!");
                    return;
                }
                // Cache user to connected clients

                if (!GameStateCache.ConnectedClients.TryAdd(userId, peer))
                {
                    logger.LogError("User is already connected!");
                    peer.Disconnect();
                }
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "User failed authentication!");
                peer.Disconnect();
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _server = new NetManager(_listener)
            {
                AutoRecycle = true
            };
            _server.Start(PORT);
            logger.LogInformation("LiteNetLib server started on port 9050");
            
            // Handler for incoming connection requests
            _listener.ConnectionRequestEvent += request =>
            {
                if (logger.IsEnabled(LogLevel.Debug))
                    logger.LogDebug("Connection request from {RemoteEndPoint}, data: {Data}", request.RemoteEndPoint, request.Data);
                request.AcceptIfKey("GameKey");
            };
            // Handler for connected peers
            _listener.PeerConnectedEvent += peer =>
            {
                if (logger.IsEnabled(LogLevel.Debug))
                    logger.LogDebug("Client connected: {Address}:{Port}, ID: {Id}, Ping: {Ping}", peer.Address, peer.Port, peer.Id, peer.Ping);
            };
            // Handler for when a client sends data to the server
            // Peer is the client
            // Reader is used to read the data sent by the client
            // Channel is the channel the data was sent on
            // Method is whether the data was sent reliably or unreliably
            _listener.NetworkReceiveEvent += async (peer, reader, channel, method) =>
            {
                var header = reader.GetByte();
                if (logger.IsEnabled(LogLevel.Debug))
                    logger.LogDebug("Client ID: {Id} sent: {Header}, channel: {Channel}, method: {Method}", peer.Id, header, channel, method);

                switch (header)
                {
                    case 0:
                        Authenticator(peer, reader.GetString());
                        return;
                    default:
                        return;
                }
            };
            // Handler for when client disconnects
            _listener.PeerDisconnectedEvent += (peer, disconnectInfo) =>
            {
                if (logger.IsEnabled(LogLevel.Debug))
                    logger.LogDebug("Client ID: {Id} disconnected, {Reason}", peer.Id, disconnectInfo.Reason);
                foreach (var connection in GameStateCache.ConnectedClients)
                {
                    if (ReferenceEquals(connection.Value, peer) && GameStateCache.ConnectedClients.TryRemove(connection.Key, out _))
                    {
                        if (logger.IsEnabled(LogLevel.Debug))
                            logger.LogDebug("User {UserId} disconnect: {Reason}", connection.Key, disconnectInfo.Reason);
                        break;
                    }
                }
            };

            while (!stoppingToken.IsCancellationRequested)
            {
                _server.PollEvents();
                await Task.Delay(10, stoppingToken);
            }
        }
    }
}