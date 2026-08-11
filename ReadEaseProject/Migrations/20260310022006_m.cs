using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReadEase.Migrations
{
    /// <inheritdoc />
    public partial class m : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BookReviews_UserId_BookId",
                table: "BookReviews");

            migrationBuilder.AlterColumn<string>(
                name: "Text",
                table: "BookReviews",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(1200)",
                oldMaxLength: 1200,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_BookReviews_UserId",
                table: "BookReviews",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BookReviews_UserId",
                table: "BookReviews");

            migrationBuilder.AlterColumn<string>(
                name: "Text",
                table: "BookReviews",
                type: "nvarchar(1200)",
                maxLength: 1200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_BookReviews_UserId_BookId",
                table: "BookReviews",
                columns: new[] { "UserId", "BookId" },
                unique: true);
        }
    }
}
