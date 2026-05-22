using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace ChatRoomAPI.Hubs
{
    public class ChatHub : Hub    
    {
        // When a client connects to the hub
        public override async Task OnConnectedAsync()
        {
            await base.OnConnectedAsync();
        }
        // When a client disconnects from the hub
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await base.OnDisconnectedAsync(exception);
        }
        // When a client sends a message to the hub
        public async Task SendMessage(string user, string message)
        {
            await Clients.All.SendAsync("ReceiveMessage", user, message);
        }
        // When a client receives a message from the hub
        public async Task ReceiveMessage(string user, string message)
        {
            await Clients.All.SendAsync("ReceiveMessage", user, message);
        }
        // When a client joins a room
        public async Task JoinRoom(string roomName)
        {
            await Clients.All.SendAsync("ReceiveMessage", "System", $" {roomName}");
        }
        // When a client leaves a room
        public async Task LeaveRoom(string roomName)
        {
            await Clients.All.SendAsync("ReceiveMessage", "System", $" {roomName}");
        }
    }
}