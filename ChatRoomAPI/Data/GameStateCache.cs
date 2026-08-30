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
        public static readonly ConcurrentDictionary<int, int> UserToRoom = new();
        // Map of game states for each room, roomId -> GameState
        public static readonly ConcurrentDictionary<int, GameStateDto> GameStates = new();
    }
}