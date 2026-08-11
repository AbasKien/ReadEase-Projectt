using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReadEase.Migrations
{
    /// <inheritdoc />
    public partial class wwwwwww : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "Books",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Books",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "Books");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Books");
        }
    }
}
