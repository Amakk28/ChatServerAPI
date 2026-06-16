using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ChatRoomAPI.Models
{
    public class GameState
    {
        public int RoomId { get; set; }
        public required string CurrentTurnPlayerId { get; set; }
        public int TurnNumber { get; set; }
        public List<Unit> Units { get; set; } = new();  
    }

    public class Unit
    {
        public int Id { get; set; }
        public required string OwnerPlayerId { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Health { get; set; }
        public bool HasMoved { get; set; }
        
        public string GameStateRoomId { get; set; } = null!; // Foreign key to GameState.RoomId
        public GameState GameState { get; set; } = null!; // Navigation property
    }
}