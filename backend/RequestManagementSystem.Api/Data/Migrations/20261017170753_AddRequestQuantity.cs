using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RequestManagementSystem.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRequestQuantity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Quantity",
                table: "Requests",
                type: "int",
                nullable: false,
                defaultValue: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "Requests");
        }
    }
}
