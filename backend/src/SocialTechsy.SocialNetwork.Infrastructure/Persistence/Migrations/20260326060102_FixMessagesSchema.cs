using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SocialTechsy.SocialNetwork.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixMessagesSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // === MessageReactions: drop FK (phu thuoc Messages.MessageId) ===
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_MessageReactions_Messages_MessageId')
                    ALTER TABLE [MessageReactions] DROP CONSTRAINT [FK_MessageReactions_Messages_MessageId];
            ");

            // === Messages: drop self-ref FK (phu thuoc ReplyToMessageId) ===
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Messages_Messages_ReplyToMessageId')
                    ALTER TABLE [Messages] DROP CONSTRAINT [FK_Messages_Messages_ReplyToMessageId];
            ");

            // === Messages: drop PK (phu thuoc MessageId) ===
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK_Messages')
                    ALTER TABLE [Messages] DROP CONSTRAINT [PK_Messages];
            ");

            // === Messages: drop index tren ReplyToMessageId (phu thuoc cot) ===
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Messages_ReplyToMessageId' AND object_id = OBJECT_ID('Messages'))
                    DROP INDEX [IX_Messages_ReplyToMessageId] ON [Messages];
            ");

            // === Messages: doi MessageId int -> bigint ===
            migrationBuilder.Sql(@"
                ALTER TABLE [Messages] ALTER COLUMN [MessageId] bigint NOT NULL;
            ");

            // === Messages: doi ReplyToMessageId int -> bigint ===
            migrationBuilder.Sql(@"
                ALTER TABLE [Messages] ALTER COLUMN [ReplyToMessageId] bigint NULL;
            ");

            // === Messages: tao lai PK voi bigint ===
            migrationBuilder.Sql(@"
                ALTER TABLE [Messages] ADD CONSTRAINT [PK_Messages] PRIMARY KEY ([MessageId]);
            ");

            // === Messages: tao lai index tren ReplyToMessageId (bigint) ===
            migrationBuilder.Sql(@"
                CREATE INDEX [IX_Messages_ReplyToMessageId] ON [Messages] ([ReplyToMessageId]);
            ");

            // === Messages: tao lai self-ref FK ===
            migrationBuilder.Sql(@"
                ALTER TABLE [Messages]
                ADD CONSTRAINT [FK_Messages_Messages_ReplyToMessageId]
                FOREIGN KEY ([ReplyToMessageId]) REFERENCES [Messages] ([MessageId])
                ON DELETE NO ACTION;
            ");

            // === MessageReactions: drop unique index (phu thuoc MessageId) ===
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_MessageReactions_MessageId_UserId' AND object_id = OBJECT_ID('MessageReactions'))
                    DROP INDEX [IX_MessageReactions_MessageId_UserId] ON [MessageReactions];
            ");

            // === MessageReactions: doi MessageId int -> bigint ===
            migrationBuilder.Sql(@"
                ALTER TABLE [MessageReactions] ALTER COLUMN [MessageId] bigint NOT NULL;
            ");

            // === MessageReactions: tao lai unique index voi bigint ===
            migrationBuilder.Sql(@"
                CREATE UNIQUE INDEX [IX_MessageReactions_MessageId_UserId]
                    ON [MessageReactions] ([MessageId], [UserId]);
            ");

            // === MessageReactions: tao lai FK ===
            migrationBuilder.Sql(@"
                ALTER TABLE [MessageReactions]
                ADD CONSTRAINT [FK_MessageReactions_Messages_MessageId]
                FOREIGN KEY ([MessageId]) REFERENCES [Messages] ([MessageId])
                ON DELETE CASCADE;
            ");

            // === Messages: them DeliveryStatus (cot thieu trong DB) ===
            migrationBuilder.AddColumn<int>(
                name: "DeliveryStatus",
                table: "Messages",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // === Messages: xoa DeliveryStatus ===
            migrationBuilder.DropColumn(name: "DeliveryStatus", table: "Messages");

            // === MessageReactions: drop FK ===
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_MessageReactions_Messages_MessageId')
                    ALTER TABLE [MessageReactions] DROP CONSTRAINT [FK_MessageReactions_Messages_MessageId];
            ");

            // === MessageReactions: drop unique index (bigint) ===
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_MessageReactions_MessageId_UserId' AND object_id = OBJECT_ID('MessageReactions'))
                    DROP INDEX [IX_MessageReactions_MessageId_UserId] ON [MessageReactions];
            ");

            // === MessageReactions: doi ngược MessageId bigint -> int ===
            migrationBuilder.Sql(@"
                ALTER TABLE [MessageReactions] ALTER COLUMN [MessageId] int NOT NULL;
            ");

            // === MessageReactions: tao lai unique index voi int ===
            migrationBuilder.Sql(@"
                CREATE UNIQUE INDEX [IX_MessageReactions_MessageId_UserId]
                    ON [MessageReactions] ([MessageId], [UserId]);
            ");

            // === Messages: drop self-ref FK ===
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Messages_Messages_ReplyToMessageId')
                    ALTER TABLE [Messages] DROP CONSTRAINT [FK_Messages_Messages_ReplyToMessageId];
            ");

            // === Messages: drop index tren ReplyToMessageId (bigint) ===
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Messages_ReplyToMessageId' AND object_id = OBJECT_ID('Messages'))
                    DROP INDEX [IX_Messages_ReplyToMessageId] ON [Messages];
            ");

            // === Messages: drop PK (bigint) ===
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK_Messages')
                    ALTER TABLE [Messages] DROP CONSTRAINT [PK_Messages];
            ");

            // === Messages: doi ngược ReplyToMessageId bigint -> int ===
            migrationBuilder.Sql(@"
                ALTER TABLE [Messages] ALTER COLUMN [ReplyToMessageId] int NULL;
            ");

            // === Messages: doi ngược MessageId bigint -> int ===
            migrationBuilder.Sql(@"
                ALTER TABLE [Messages] ALTER COLUMN [MessageId] int NOT NULL;
            ");

            // === Messages: tao lai PK voi int ===
            migrationBuilder.Sql(@"
                ALTER TABLE [Messages] ADD CONSTRAINT [PK_Messages] PRIMARY KEY ([MessageId]);
            ");

            // === Messages: tao lai index tren ReplyToMessageId (int) ===
            migrationBuilder.Sql(@"
                CREATE INDEX [IX_Messages_ReplyToMessageId] ON [Messages] ([ReplyToMessageId]);
            ");

            // === Messages: tao lai self-ref FK ===
            migrationBuilder.Sql(@"
                ALTER TABLE [Messages]
                ADD CONSTRAINT [FK_Messages_Messages_ReplyToMessageId]
                FOREIGN KEY ([ReplyToMessageId]) REFERENCES [Messages] ([MessageId])
                ON DELETE NO ACTION;
            ");

            // === MessageReactions: tao lai FK ===
            migrationBuilder.Sql(@"
                ALTER TABLE [MessageReactions]
                ADD CONSTRAINT [FK_MessageReactions_Messages_MessageId]
                FOREIGN KEY ([MessageId]) REFERENCES [Messages] ([MessageId])
                ON DELETE CASCADE;
            ");
        }
    }
}
