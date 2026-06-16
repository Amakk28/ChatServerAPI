using Microsoft.EntityFrameworkCore;
using ChatRoomAPI.Models;

namespace ChatRoomAPI.Data
{
    public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Room> Rooms { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<Unit> Units { get; set; }
        public DbSet<GameState> GameStates { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<GameState>(entity =>
            {
                entity.HasKey(gs => gs.RoomId);
                entity.Property(gs => gs.RoomId).ValueGeneratedNever();
                entity.HasOne(gs => gs.Room)
                    .WithOne(r => r.GameState)
                    .HasForeignKey<GameState>(gs => gs.RoomId)
                    .IsRequired();
            });
        }
    }
}