using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ChatRoomAPI.Models.DTOs
{
    public class CreateRoomDto
    {
        public string Name { get; set; } = string.Empty;
    }
}