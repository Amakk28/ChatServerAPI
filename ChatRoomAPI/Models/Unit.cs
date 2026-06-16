using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ChatRoomAPI.Models
{
    public class Unit
    {
        public int Id { get; set; }
        public required int OwnerPlayerId { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Health { get; set; }
        public bool HasMoved { get; set; }
        
        public required int GameStateRoomId { get; set; } // Foreign key to GameState.RoomId
        public required GameState GameState { get; set; } // Navigation property
    }
}