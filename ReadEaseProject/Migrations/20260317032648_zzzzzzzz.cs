using System;
using Microsoft.EntityFrameworkCore.Migrations;
#nullable disable

namespace ReadEase.Migrations
{
    /// <inheritdoc />
    public partial class zzzzzzzz : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1 — Add nullable first (no default Guid.Empty, no FK yet)
            migrationBuilder.AddColumn<Guid>(
                name: "WallOwnerId",
                table: "UserPosts",
                type: "uniqueidentifier",
                nullable: true);

            // Step 2 — Copy AuthorId into WallOwnerId for all existing rows
            migrationBuilder.Sql(
                "UPDATE UserPosts SET WallOwnerId = AuthorId WHERE WallOwnerId IS NULL");

            // Step 3 — Make NOT NULL now that every row has a valid User.Id value
            migrationBuilder.AlterColumn<Guid>(
                name: "WallOwnerId",
                table: "UserPosts",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            // Step 4 — Index
            migrationBuilder.CreateIndex(
                name: "IX_UserPosts_WallOwnerId",
                table: "UserPosts",
                column: "WallOwnerId");

            // Step 5 — FK is safe now, every row points to a real User
            migrationBuilder.AddForeignKey(
                name: "FK_UserPosts_Users_WallOwnerId",
                table: "UserPosts",
                column: "WallOwnerId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserPosts_Users_WallOwnerId",
                table: "UserPosts");

            migrationBuilder.DropIndex(
                name: "IX_UserPosts_WallOwnerId",
                table: "UserPosts");

            migrationBuilder.DropColumn(
                name: "WallOwnerId",
                table: "UserPosts");
        }
    }
}