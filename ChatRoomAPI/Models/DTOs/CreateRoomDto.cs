using System.ComponentModel.DataAnnotations;

namespace ChatRoomAPI.Models.DTOs
{
    public class CreateRoomDto
    {
        public string Name { get; set; } = string.Empty;
    }
}