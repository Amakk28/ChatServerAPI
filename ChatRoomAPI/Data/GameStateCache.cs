using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ChatRoomAPI.Models.DTOs;

namespace ChatRoomAPI.Data
{
    public static class GameStateCache
    {
        // Current Memory Map of Online Users, userId -> roomId
        // roomId can be null, indicating the user is in the lobby
        static readonly ConcurrentDictionary<int, int?> OnlineUsers = new();
        // Map for userId to connectionId, each user can only have one connection at a time
        static readonly ConcurrentDictionary<int, string> UserConnections = new();
        // Map of game states for each room, roomId -> GameState
        static readonly ConcurrentDictionary<int, GameStateDto> GameStates = new();
    }
}