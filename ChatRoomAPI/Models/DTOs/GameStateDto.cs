using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ChatRoomAPI.Models.DTOs
{
    public class GameStateDto
    {
        public required int RoomId { get; set; }
        public required int OwnerId {get; set;}
        public int CurrentTurnPlayerId { get; set; }
        public int TurnNumber { get; set; }
        public List<UnitDto> Units { get; set; } = [];

    }

    public class UnitDto
    {
        public required int OwnerPlayerId { get; set; }
        public required int GameStateId {get; set;}
        public int X { get; set; }
        public int Y { get; set; }
        public int Z { get; set; }
        public int Health { get; set; }
        public bool HasMoved { get; set; }
    }
}