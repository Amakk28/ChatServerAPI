using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ChatRoomAPI.Models.DTOs;
using LiteNetLib;

namespace ChatRoomAPI.Data
{
    public static class GameStateCache
    {
        // Current Memory Map of Online Users, userId -> roomId
        // roomId can be null, indicating the user is in the lobby
        // public static readonly ConcurrentDictionary<int, int?> OnlineUsers = new();
        // Map for userId to connectionId, each user can only have one connection at a time, SignalR only
        // public static readonly ConcurrentDictionary<int, string> UserConnections = new();
        // Map of game states for each room, roomId -> GameState
        public static readonly ConcurrentDictionary<int, GameStateDto> GameStates = new();
        // Map for userId to peer, LiteNetLib service only
        public static readonly ConcurrentDictionary<int, NetPeer> ConnectedClients = new();
    }
}