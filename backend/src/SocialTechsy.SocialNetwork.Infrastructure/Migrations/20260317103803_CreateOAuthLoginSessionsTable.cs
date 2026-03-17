using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SocialTechsy.SocialNetwork.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CreateOAuthLoginSessionsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OAuthLoginSessions",
                columns: table => new
                {
                    OAuthLoginSessionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Provider = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    AccessToken = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RefreshToken = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsUsed = table.Column<bool>(type: "bit", nullable: false),
                    UsedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OAuthLoginSessions", x => x.OAuthLoginSessionId);
                    table.ForeignKey(
                        name: "FK_OAuthLoginSessions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OAuthLoginSessions_Code",
                table: "OAuthLoginSessions",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OAuthLoginSessions_ExpiresAt",
                table: "OAuthLoginSessions",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_OAuthLoginSessions_UserId",
                table: "OAuthLoginSessions",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OAuthLoginSessions");
        }
    }
}
