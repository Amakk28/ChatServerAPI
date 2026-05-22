namespace ChatRoomAPI.Models
{
    public class Message
    {
        public int Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int UserId { get; set; } // User who sent message
        public int RoomId { get; set; }
        
    }
}