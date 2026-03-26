using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SocialTechsy.SocialNetwork.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRefreshTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF OBJECT_ID(N'Messages', N'U') IS NOT NULL
                BEGIN
                    IF COL_LENGTH('Messages', 'AttachmentFileName') IS NULL
                        ALTER TABLE [Messages] ADD [AttachmentFileName] nvarchar(255) NULL;
                    IF COL_LENGTH('Messages', 'AttachmentSize') IS NULL
                        ALTER TABLE [Messages] ADD [AttachmentSize] bigint NULL;
                    IF COL_LENGTH('Messages', 'AttachmentUrl') IS NULL
                        ALTER TABLE [Messages] ADD [AttachmentUrl] nvarchar(500) NULL;
                    IF COL_LENGTH('Messages', 'MessageType') IS NULL
                        ALTER TABLE [Messages] ADD [MessageType] nvarchar(20) NOT NULL DEFAULT 'text';
                    IF COL_LENGTH('Messages', 'ReplyToMessageId') IS NULL
                        ALTER TABLE [Messages] ADD [ReplyToMessageId] int NULL;
                END
            ");

            migrationBuilder.Sql(@"
                IF OBJECT_ID(N'MessageReactions', N'U') IS NULL AND OBJECT_ID(N'Messages', N'U') IS NOT NULL
                BEGIN
                    CREATE TABLE [MessageReactions] (
                        [MessageReactionId] int NOT NULL IDENTITY,
                        [ReactionType] nvarchar(20) NOT NULL,
                        [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
                        [MessageId] int NOT NULL,
                        [UserId] int NOT NULL,
                        CONSTRAINT [PK_MessageReactions] PRIMARY KEY ([MessageReactionId]),
                        CONSTRAINT [FK_MessageReactions_Messages_MessageId] FOREIGN KEY ([MessageId]) REFERENCES [Messages] ([MessageId]) ON DELETE CASCADE,
                        CONSTRAINT [FK_MessageReactions_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([UserId]) ON DELETE NO ACTION
                    );
                END
            ");

            migrationBuilder.Sql(@"
                IF OBJECT_ID(N'RefreshTokens', N'U') IS NULL AND OBJECT_ID(N'Users', N'U') IS NOT NULL
                BEGIN
                    CREATE TABLE [RefreshTokens] (
                        [RefreshTokenId] int NOT NULL IDENTITY,
                        [Token] nvarchar(500) NOT NULL,
                        [UserId] int NOT NULL,
                        [ExpiresAt] datetime2 NOT NULL,
                        [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
                        [RevokedAt] datetime2 NULL,
                        [ReplacedByToken] nvarchar(500) NULL,
                        CONSTRAINT [PK_RefreshTokens] PRIMARY KEY ([RefreshTokenId]),
                        CONSTRAINT [FK_RefreshTokens_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([UserId]) ON DELETE CASCADE
                    );
                END
            ");

            migrationBuilder.Sql(@"
                IF OBJECT_ID(N'Messages', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Messages_ReplyToMessageId')
                    CREATE INDEX [IX_Messages_ReplyToMessageId] ON [Messages] ([ReplyToMessageId]);
            ");

            migrationBuilder.Sql(@"
                IF OBJECT_ID(N'MessageReactions', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_MessageReactions_MessageId_UserId')
                    CREATE UNIQUE INDEX [IX_MessageReactions_MessageId_UserId] ON [MessageReactions] ([MessageId], [UserId]);
            ");

            migrationBuilder.Sql(@"
                IF OBJECT_ID(N'MessageReactions', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_MessageReactions_UserId')
                    CREATE INDEX [IX_MessageReactions_UserId] ON [MessageReactions] ([UserId]);
            ");

            migrationBuilder.Sql(@"
                IF OBJECT_ID(N'RefreshTokens', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_RefreshTokens_ExpiresAt')
                    CREATE INDEX [IX_RefreshTokens_ExpiresAt] ON [RefreshTokens] ([ExpiresAt]);
            ");

            migrationBuilder.Sql(@"
                IF OBJECT_ID(N'RefreshTokens', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_RefreshTokens_Token')
                    CREATE UNIQUE INDEX [IX_RefreshTokens_Token] ON [RefreshTokens] ([Token]);
            ");

            migrationBuilder.Sql(@"
                IF OBJECT_ID(N'RefreshTokens', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_RefreshTokens_UserId')
                    CREATE INDEX [IX_RefreshTokens_UserId] ON [RefreshTokens] ([UserId]);
            ");

            migrationBuilder.Sql(@"
                IF OBJECT_ID(N'Messages', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Messages_Messages_ReplyToMessageId')
                    ALTER TABLE [Messages] ADD CONSTRAINT [FK_Messages_Messages_ReplyToMessageId]
                        FOREIGN KEY ([ReplyToMessageId]) REFERENCES [Messages] ([MessageId]) ON DELETE SET NULL;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Messages_Messages_ReplyToMessageId",
                table: "Messages");

            migrationBuilder.DropTable(
                name: "MessageReactions");

            migrationBuilder.DropTable(
                name: "RefreshTokens");

            migrationBuilder.DropIndex(
                name: "IX_Messages_ReplyToMessageId",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "AttachmentFileName",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "AttachmentSize",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "AttachmentUrl",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "MessageType",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "ReplyToMessageId",
                table: "Messages");
        }
    }
}
