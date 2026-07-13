using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace ChatRoomAPI.Models
{
    public class GameState
    {
        [Key]
        public required int RoomId { get; set; }
        public required int OwnerId {get; set;}
        public Room? Room { get; set; } // Navigation property to Room
        public required int CurrentTurnPlayerId { get; set; }
        public int TurnNumber { get; set; }
        public List<Unit> Units { get; set; } = [];
    }
}