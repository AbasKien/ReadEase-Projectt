using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReadEase.Migrations
{
    /// <inheritdoc />
    public partial class kakakaala : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "PremiumUnlocksAt",
                table: "BookChapters",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PremiumUnlocksAt",
                table: "BookChapters");
        }
    }
}
