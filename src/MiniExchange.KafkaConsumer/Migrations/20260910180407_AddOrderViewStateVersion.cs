using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiniExchange.KafkaConsumer.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderViewStateVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "StateVersion",
                table: "OrderViews",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StateVersion",
                table: "OrderViews");
        }
    }
}
