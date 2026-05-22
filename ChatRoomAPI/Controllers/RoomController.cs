using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ChatRoomAPI.Data;
using ChatRoomAPI.Models;
using ChatRoomAPI.Models.DTOs;
using ChatRoomAPI.Hubs;

namespace ChatRoomAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class RoomController(AppDbContext context) : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> GetRooms()
        {
            var rooms = await context.Rooms.ToListAsync();
            return Ok(rooms);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetRoom(int id)
        {
            var room = await context.Rooms.FindAsync(id);
            if (room == null)
            {
                return NotFound("Room not found.");
            }
            return Ok(room);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateRoom(CreateRoomDto createRoomDto)
        {
            var room = new Room
            {
                Name = createRoomDto.Name
            };
            context.Rooms.Add(room);
            await context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetRoom), new { id = room.Id }, room);
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateRoom(int id) 
        {
            var room = await context.Rooms.FindAsync(id);
            if (room == null)
            {
                return NotFound("Room not found.");
            }
            
            context.Rooms.Remove(room);
            await context.SaveChangesAsync();
            return NoContent();
        }

    }
}