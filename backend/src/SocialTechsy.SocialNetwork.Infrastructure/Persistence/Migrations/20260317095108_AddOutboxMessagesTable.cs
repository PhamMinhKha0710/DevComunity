using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SocialTechsy.SocialNetwork.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOutboxMessagesTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'OutboxMessages')
                BEGIN
                    CREATE TABLE [OutboxMessages] (
                        [Id] bigint NOT NULL IDENTITY(1,1),
                        [EventType] nvarchar(200) NOT NULL,
                        [PayloadJson] nvarchar(max) NOT NULL,
                        [CreatedAt] datetime2 NOT NULL DEFAULT GETUTCDATE(),
                        [ProcessedAt] datetime2 NULL,
                        [RetryCount] int NOT NULL DEFAULT 0,
                        CONSTRAINT [PK_OutboxMessages] PRIMARY KEY ([Id])
                    );

                    CREATE NONCLUSTERED INDEX [IX_OutboxMessages_Pending] 
                    ON [OutboxMessages] ([ProcessedAt], [RetryCount])
                    WHERE [ProcessedAt] IS NULL AND [RetryCount] < 5;
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.tables WHERE name = 'OutboxMessages')
                BEGIN
                    DROP TABLE [OutboxMessages];
                END
            ");
        }
    }
}
