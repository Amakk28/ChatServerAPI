using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChatRoomAPI.Migrations
{
    /// <inheritdoc />
    public partial class unitupdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Units_GameStates_GameStateRoomId",
                table: "Units");

            migrationBuilder.RenameColumn(
                name: "GameStateRoomId",
                table: "Units",
                newName: "GameStateId");

            migrationBuilder.RenameIndex(
                name: "IX_Units_GameStateRoomId",
                table: "Units",
                newName: "IX_Units_GameStateId");

            migrationBuilder.AddForeignKey(
                name: "FK_Units_GameStates_GameStateId",
                table: "Units",
                column: "GameStateId",
                principalTable: "GameStates",
                principalColumn: "RoomId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Units_GameStates_GameStateId",
                table: "Units");

            migrationBuilder.RenameColumn(
                name: "GameStateId",
                table: "Units",
                newName: "GameStateRoomId");

            migrationBuilder.RenameIndex(
                name: "IX_Units_GameStateId",
                table: "Units",
                newName: "IX_Units_GameStateRoomId");

            migrationBuilder.AddForeignKey(
                name: "FK_Units_GameStates_GameStateRoomId",
                table: "Units",
                column: "GameStateRoomId",
                principalTable: "GameStates",
                principalColumn: "RoomId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
