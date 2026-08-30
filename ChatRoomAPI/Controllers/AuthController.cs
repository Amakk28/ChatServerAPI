using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authorization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using ChatRoomAPI.Data;
using ChatRoomAPI.Models;
using ChatRoomAPI.Models.DTOs;


namespace ChatRoomAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController(AppDbContext context, IConfiguration configuration, ILogger<AuthController> logger) : ControllerBase
    {
        // Profile Lookup endpoint
        [HttpGet("profile")]
        [Authorize]
        public async Task<IActionResult> GetProfile()
        {
            var userId = User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (userId == null)
            {
                return Unauthorized("Invalid token.");
            }
            var user = await context.Users.FindAsync(int.Parse(userId));
            if (user == null)
            {
                return NotFound("User not found.");
            }
            return Ok(new { user.Username, user.Email });
        }

        // Registration 
        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto registerDto)
        {
            if (await context.Users.AnyAsync(u => u.Email == registerDto.Email))
            {
                logger.LogWarning("Email already in use.");
                return BadRequest("Email already in use.");
            }

            var user = new User
            {
                Username = registerDto.Username,
                Email = registerDto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(registerDto.Password),
                TextColor = "white"
            };

            context.Users.Add(user);
            await context.SaveChangesAsync();
            logger.LogDebug("User registered successfully.");
            return Ok("User registered successfully.");
        }

        // Login endpoint
        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto loginDto)
        {
            if (logger.IsEnabled(LogLevel.Debug))
                logger.LogDebug("Login attempt for email: {loginDto.Email}", loginDto.Email);
            var user = await context.Users.FirstOrDefaultAsync(u => u.Email == loginDto.Email);
            if (user == null || !BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
            {
                logger.LogError("Invalid email or password.");
                return Unauthorized("Invalid email or password.");
            }
            logger.LogDebug("Login succesful");
            var token = GenerateToken(user);
            return Ok(new LoginResponseDto { Token = token, User = new UserDto { Id = user.Id, Email = user.Email, Username = user.Username, TextColor = user.TextColor } });
        }

        private string GenerateToken(User user)
        {
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            };

            var key = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(Environment.GetEnvironmentVariable("JWT_KEY") ?? configuration["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: Environment.GetEnvironmentVariable("JWT_ISSUER") ?? configuration["Jwt:Issuer"],
                audience: Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddHours(1),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}