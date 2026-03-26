using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SocialTechsy.SocialNetwork.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTagPreferenceFollowedIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_TagPreferences_UserId_IsFollowed",
                table: "TagPreferences",
                columns: new[] { "UserId", "IsFollowed" },
                filter: "[IsFollowed] = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TagPreferences_UserId_IsFollowed",
                table: "TagPreferences");
        }
    }
}
