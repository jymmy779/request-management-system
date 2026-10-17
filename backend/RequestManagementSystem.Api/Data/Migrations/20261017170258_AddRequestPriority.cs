using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RequestManagementSystem.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRequestPriority : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Priority",
                table: "Requests",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Medium");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Priority",
                table: "Requests");
        }
    }
}
