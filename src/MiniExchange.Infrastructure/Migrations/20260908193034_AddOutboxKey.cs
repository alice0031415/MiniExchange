using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiniExchange.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOutboxKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Key",
                table: "OutboxMessages",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Key",
                table: "OutboxMessages");
        }
    }
}
