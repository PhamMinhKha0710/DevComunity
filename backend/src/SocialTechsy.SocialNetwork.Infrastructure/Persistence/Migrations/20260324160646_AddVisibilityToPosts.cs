using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SocialTechsy.SocialNetwork.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVisibilityToPosts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Visibility",
                table: "Posts",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Visibility",
                table: "Posts");
        }
    }
}
