using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReadEase.Migrations
{
    /// <inheritdoc />
    public partial class zzzz : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReplyingTo",
                table: "ReviewComments",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReplyingTo",
                table: "ReviewComments");
        }
    }
}
