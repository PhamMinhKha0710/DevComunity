using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SocialTechsy.SocialNetwork.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixMissingPostIdColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PostId",
                table: "SavedItems",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PostId",
                table: "Comments",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SavedItems_PostId",
                table: "SavedItems",
                column: "PostId");

            migrationBuilder.CreateIndex(
                name: "IX_SavedItems_UserId_PostId",
                table: "SavedItems",
                columns: new[] { "UserId", "PostId" },
                filter: "[PostId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Comments_PostId",
                table: "Comments",
                column: "PostId");

            migrationBuilder.AddForeignKey(
                name: "FK_Comments_Posts_PostId",
                table: "Comments",
                column: "PostId",
                principalTable: "Posts",
                principalColumn: "PostId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SavedItems_Posts_PostId",
                table: "SavedItems",
                column: "PostId",
                principalTable: "Posts",
                principalColumn: "PostId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Comments_Posts_PostId",
                table: "Comments");

            migrationBuilder.DropForeignKey(
                name: "FK_SavedItems_Posts_PostId",
                table: "SavedItems");

            migrationBuilder.DropIndex(
                name: "IX_SavedItems_PostId",
                table: "SavedItems");

            migrationBuilder.DropIndex(
                name: "IX_SavedItems_UserId_PostId",
                table: "SavedItems");

            migrationBuilder.DropIndex(
                name: "IX_Comments_PostId",
                table: "Comments");

            migrationBuilder.DropColumn(
                name: "PostId",
                table: "SavedItems");

            migrationBuilder.DropColumn(
                name: "PostId",
                table: "Comments");
        }
    }
}
