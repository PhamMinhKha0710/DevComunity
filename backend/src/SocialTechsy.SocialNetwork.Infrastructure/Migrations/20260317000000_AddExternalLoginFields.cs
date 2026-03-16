using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SocialTechsy.SocialNetwork.Infrastructure.Migrations
{
    /// <summary>
    /// Adds external login (OAuth) fields to Users table
    /// </summary>
    public partial class AddExternalLoginFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExternalProvider",
                table: "Users",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalProviderId",
                table: "Users",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalProviderAvatar",
                table: "Users",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.Sql(@"
                CREATE UNIQUE INDEX IX_Users_ExternalProvider_ExternalProviderId
                ON Users (ExternalProvider, ExternalProviderId)
                WHERE ExternalProvider IS NOT NULL AND ExternalProviderId IS NOT NULL;
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS IX_Users_ExternalProvider_ExternalProviderId ON Users;");

            migrationBuilder.DropColumn(name: "ExternalProvider", table: "Users");
            migrationBuilder.DropColumn(name: "ExternalProviderId", table: "Users");
            migrationBuilder.DropColumn(name: "ExternalProviderAvatar", table: "Users");
        }
    }
}
