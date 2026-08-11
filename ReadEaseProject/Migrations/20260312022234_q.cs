using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReadEase.Migrations
{
    /// <inheritdoc />
    public partial class q : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BookReviews_UserId",
                table: "BookReviews");

            migrationBuilder.AddColumn<DateTime>(
                name: "AuthorRepliedAt",
                table: "BookReviews",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AuthorReply",
                table: "BookReviews",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_BookReviews_UserId_BookId",
                table: "BookReviews",
                columns: new[] { "UserId", "BookId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BookReviews_UserId_BookId",
                table: "BookReviews");

            migrationBuilder.DropColumn(
                name: "AuthorRepliedAt",
                table: "BookReviews");

            migrationBuilder.DropColumn(
                name: "AuthorReply",
                table: "BookReviews");

            migrationBuilder.CreateIndex(
                name: "IX_BookReviews_UserId",
                table: "BookReviews",
                column: "UserId");
        }
    }
}
