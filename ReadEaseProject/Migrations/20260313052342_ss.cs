using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReadEase.Migrations
{
    /// <inheritdoc />
    public partial class ss : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReviewCommentLikes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CommentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReviewCommentLikes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReviewCommentLikes_ReviewComments_CommentId",
                        column: x => x.CommentId,
                        principalTable: "ReviewComments",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ReviewCommentLikes_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReviewCommentLikes_CommentId",
                table: "ReviewCommentLikes",
                column: "CommentId");

            migrationBuilder.CreateIndex(
                name: "IX_ReviewCommentLikes_UserId_CommentId",
                table: "ReviewCommentLikes",
                columns: new[] { "UserId", "CommentId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReviewCommentLikes");
        }
    }
}
