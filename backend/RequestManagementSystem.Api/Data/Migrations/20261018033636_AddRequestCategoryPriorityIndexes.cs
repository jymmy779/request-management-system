using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RequestManagementSystem.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRequestCategoryPriorityIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Category",
                table: "Requests",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_Requests_Category_CreatedAt_Id",
                table: "Requests",
                columns: new[] { "Category", "CreatedAt", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_Requests_Priority_CreatedAt_Id",
                table: "Requests",
                columns: new[] { "Priority", "CreatedAt", "Id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Requests_Category_CreatedAt_Id",
                table: "Requests");

            migrationBuilder.DropIndex(
                name: "IX_Requests_Priority_CreatedAt_Id",
                table: "Requests");

            migrationBuilder.AlterColumn<string>(
                name: "Category",
                table: "Requests",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);
        }
    }
}
