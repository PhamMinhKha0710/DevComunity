using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SocialTechsy.SocialNetwork.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddExternalProviderColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExternalProvider",
                table: "Users",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalProviderAvatar",
                table: "Users",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalProviderId",
                table: "Users",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_ExternalProvider_ExternalProviderId",
                table: "Users",
                columns: new[] { "ExternalProvider", "ExternalProviderId" },
                unique: true,
                filter: "[ExternalProvider] IS NOT NULL AND [ExternalProviderId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_ExternalProvider_ExternalProviderId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ExternalProvider",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ExternalProviderAvatar",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ExternalProviderId",
                table: "Users");
        }
    }
}
