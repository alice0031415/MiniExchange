using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiniExchange.KafkaConsumer.Migrations
{
    /// <inheritdoc />
    public partial class RemoveOrderViewStateVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StateVersion",
                table: "OrderViews");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "StateVersion",
                table: "OrderViews",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
