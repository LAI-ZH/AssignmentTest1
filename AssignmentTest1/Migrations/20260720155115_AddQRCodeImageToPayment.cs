using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AssignmentTest1.Migrations
{
    /// <inheritdoc />
    public partial class AddQRCodeImageToPayment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "QRCodeImage",
                table: "Payments",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "QRCodeImage",
                table: "Payments");
        }
    }
}
