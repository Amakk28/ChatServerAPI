using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using LiteNetLib;

namespace ChatRoomAPI.Services
{
    public class LiteNetLibService : BackgroundService
    {
        private EventBasedNetListener _listener = new();
        private NetManager? _server;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _server = new NetManager(_listener);
            _server.Start(9050);
            
            // Handler for incoming connection requests
            _listener.ConnectionRequestEvent += request =>
            {
                request.AcceptIfKey("GameKey");
            };
            // Handler for connected peers
            _listener.PeerConnectedEvent += peer =>
            {
                Console.WriteLine($"Client connected: {peer.Address}:{peer.Port}");
            };
            // Handler for when a client sends data to the server
            // Peer is the client
            // Reader is used to read the data sent by the client
            // Channel is the channel the data was sent on
            // Method is whether the data was sent reliably or unreliably
            _listener.NetworkReceiveEvent += (peer, reader, channel, method) =>
            {
                reader.Recycle();
            };
            while (!stoppingToken.IsCancellationRequested)
            {
                _server?.PollEvents();
                await Task.Delay(10, stoppingToken);
            }
        }
    }
}