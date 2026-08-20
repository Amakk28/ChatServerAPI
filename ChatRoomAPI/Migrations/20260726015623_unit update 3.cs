using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChatRoomAPI.Migrations
{
    /// <inheritdoc />
    public partial class unitupdate3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Z",
                table: "Units",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Z",
                table: "Units");
        }
    }
}
