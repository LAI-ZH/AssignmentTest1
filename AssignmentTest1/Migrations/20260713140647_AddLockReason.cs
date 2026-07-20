using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AssignmentTest1.Migrations
{
    /// <inheritdoc />
    public partial class AddLockReason : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LockReason",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LockReason",
                table: "Users");
        }
    }
}
