using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ChatRoomAPI.Models.DTOs
{
    public class ChatMessageDto
    {
        public int Id { get; set; }
        public int RoomId { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public required string SenderUsername { get; set; } 
        public required string TextColor { get; set; } 
    }
}