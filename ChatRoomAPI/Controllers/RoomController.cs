using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ChatRoomAPI.Data;
using ChatRoomAPI.Models;
using ChatRoomAPI.Models.DTOs;
using Microsoft.VisualBasic;
using System.IdentityModel.Tokens.Jwt;
// using ChatRoomAPI.Hubs;


namespace ChatRoomAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class RoomController(AppDbContext context, ILogger<RoomController> logger) : ControllerBase
    {
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GetRooms()
        {
            var rooms = await context.Rooms.ToListAsync();
        
            var roomsWithOnlineCounts = rooms.Select(room =>
            new RoomDto
            {
                Id = room.Id,
                Name = room.Name,
                CreatedAt = room.CreatedAt,
                OnlineUsers = GameStateCache.UserToRoom.Values.Count(roomId => roomId == room.Id)
            });

            return Ok(roomsWithOnlineCounts);
        }

        [AllowAnonymous]
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

        [HttpGet("{id}/gamestate")]
        [Authorize]
        public async Task<IActionResult> GetRoomGameState(int id)
        {
            var gameState = await context.GameStates.Include(gs => gs.Units).FirstOrDefaultAsync(gs => gs.RoomId == id);
            if (gameState == null)
            {
                logger.LogWarning("GameState not found!");
                return NotFound("GameState not found.");
            }

            GameStateDto gameStateDto = new()
            {
                RoomId = gameState.RoomId,
                OwnerId = gameState.OwnerId,
                CurrentTurnPlayerId = gameState.CurrentTurnPlayerId,
                TurnNumber = gameState.TurnNumber,
                Units = [.. gameState.Units.Select(u => new UnitDto
                {
                    Name = u.Name,
                    Type = u.Type,
                    OwnerPlayerId = u.OwnerPlayerId,
                    GameStateId = u.GameStateId,
                    X = u.X,
                    Y = u.Y,
                    Z = u.Z,
                    RotationY = u.RotationY,
                    HasMoved = u.HasMoved,
                    Health = u.Health
                })]
            };

            return Ok(gameStateDto);
        }

        [HttpGet("{id}/units")]
        public async Task<IActionResult> GetRoomUnits(int id)
        {
            var units = await context.Units
                .Where(u => u.GameStateId == id)
                .ToListAsync();
            return Ok(units);
        }

        [HttpGet("{id}/messages")]
        public async Task<IActionResult> GetRoomMessages(int id)
        {
            var room = await context.Rooms.FindAsync(id);
            if (room == null)
            {
                return NotFound("Room not found.");
            }
            var messages = await context.Messages
                .Where(m => m.RoomId == id)
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();
            return Ok(messages);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateRoom(CreateRoomDto createRoomDto)
        {
            // Check if host user matches the authenticated user
            var userId = int.Parse(User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ?? "0");
            if (createRoomDto.HostUserId != userId)
            {
                logger.LogWarning("Host user ID does not match authenticated user.");
                return Forbid("Host user ID does not match authenticated user.");
            }
            // So when the user creates a new room, we also create a new GameState for that room
            var room = new Room
            {
                HostUserId = createRoomDto.HostUserId,
                Name = createRoomDto.Name,
                CreatedAt = DateTime.UtcNow
            };
            
            var gameState = new GameState
            {
                OwnerId = createRoomDto.HostUserId,
                CurrentTurnPlayerId = createRoomDto.HostUserId,
                TurnNumber = 0,
                Room = room // Set the navigation property
            };
            context.Rooms.Add(room);
            context.GameStates.Add(gameState); // Add the GameState to the context
            await context.SaveChangesAsync();
            return Ok();
        }

        // Register a created Unit to the current GameState of the room
        [HttpPost("{id}/units")]
        [Authorize]
        public async Task<IActionResult> CreateUnit(int id, UnitDto createUnitDto)
        {
            var userId = int.Parse(User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ?? "0");
            var gameState = await context.GameStates.FindAsync(id);
            if (gameState == null)
            {
                return NotFound("GameState not found for the specified room.");
            }

            var unit = new Unit
            {
                Name = createUnitDto.Name,
                Type = createUnitDto.Type,
                OwnerPlayerId = userId,
                X = createUnitDto.X,
                Y = createUnitDto.Y,
                Z = createUnitDto.Z,
                RotationY = createUnitDto.RotationY,
                Health = createUnitDto.Health,
                HasMoved = createUnitDto.HasMoved,
                GameStateId = id,
                GameState = gameState
            };
            context.Units.Add(unit);
            await context.SaveChangesAsync();

            var createdUnitDto = new UnitDto
            {
                Name = unit.Name,
                Type = unit.Type,
                OwnerPlayerId = unit.OwnerPlayerId,
                GameStateId = unit.GameStateId,
                X = unit.X,
                Y = unit.Y,
                Z = unit.Z,
                RotationY = unit.RotationY,
                Health = unit.Health,
                HasMoved = unit.HasMoved
            };

            if (GameStateCache.GameStates.TryGetValue(gameState.RoomId, out var cachedGameState))
            {
                cachedGameState.Units.Add(createdUnitDto);
            }

            return Ok(createdUnitDto);
        }
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteRoom(int id)
        {
            // Check if host user matches the authenticated user
            var userId = int.Parse(User.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value ?? "0");
            if (!await context.Rooms.AnyAsync(r => r.Id == id && r.HostUserId == userId))
            {
                return Forbid("Host user ID does not match authenticated user.");
            }
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