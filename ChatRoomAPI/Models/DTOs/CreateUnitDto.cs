using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ChatRoomAPI.Models.DTOs
{
    public class CreateUnitDto
    {
        public required int OwnerPlayerId { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Health { get; set; }
        public bool HasMoved { get; set; }
        public required int GameStateRoomId { get; set; } // Foreign key to GameState.RoomId
        
    }
}