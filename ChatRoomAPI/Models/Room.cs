namespace ChatRoomAPI.Models
{
    public class Room
    {
        public int Id { get; set; }
        public int HostUserId { get; set; } // User who created the room
        public GameState? GameState { get; set; }
        public required string Name { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}