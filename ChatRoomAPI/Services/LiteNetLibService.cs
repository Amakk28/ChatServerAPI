using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using LiteNetLib;
using ChatRoomAPI.Data;
using ChatRoomAPI.Models.DTOs;
using System.IdentityModel.Tokens.Jwt;

namespace ChatRoomAPI.Services
{
    public class LiteNetLibService(IServiceScopeFactory serviceScopeFactory, ILogger<LiteNetLibService> logger) : BackgroundService
    {
        const int PORT = 9050;
        private EventBasedNetListener _listener = new();
        private NetManager? _server;
        
        private async Task Authenticator(NetPeer peer, string token)
        {
            try
            {
                var handler = new JwtSecurityTokenHandler();
                var jwt = handler.ReadJwtToken(token);

                var userIdClaim = jwt.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub);
                if (userIdClaim == null)
                {
                    peer.Disconnect();
                    return;
                }

                int userId = int.Parse(userIdClaim.Value);

                using var scope = serviceScopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var user = await db.Users.FindAsync(userId);
                if (user == null)
                {
                    peer.Disconnect();
                    return;
                }
                // Cache user to connected clients
            }
            catch
            {
                peer.Disconnect();
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _server = new NetManager(_listener);
            _server.AutoRecycle = true;
            _server.Start(PORT);
            // Console.WriteLine("~~~ LiteNetLib server started on port 9050");
            logger.LogInformation("LiteNetLib server started on port 9050");
            
            // Handler for incoming connection requests
            _listener.ConnectionRequestEvent += request =>
            {
                logger.LogInformation($"Connection request from {request.RemoteEndPoint}, data: {request.Data}");
                request.AcceptIfKey("GameKey");
            };
            // Handler for connected peers
            _listener.PeerConnectedEvent += async peer =>
            {
                Console.WriteLine($"~~~ Client connected: {peer.Address}:{peer.Port}, ID: {peer.Id}");
            };
            // Handler for when a client sends data to the server
            // Peer is the client
            // Reader is used to read the data sent by the client
            // Channel is the channel the data was sent on
            // Method is whether the data was sent reliably or unreliably
            _listener.NetworkReceiveEvent += async (peer, reader, channel, method) =>
            {
                var header = reader.GetString();
                Console.WriteLine($"~~~ Client ID: {peer.Id} sent: {header}, channel: {channel}, method: {method}");

                if (header == "AUTH"){
                    var token = reader.GetString();
                    Console.WriteLine($"Token: {token}");
                    await Authenticator(peer, token);
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