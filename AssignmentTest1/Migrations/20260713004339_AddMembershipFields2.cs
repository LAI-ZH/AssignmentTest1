using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AssignmentTest1.Migrations
{
    /// <inheritdoc />
    public partial class AddMembershipFields2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Benefits",
                table: "MembershipPlans",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Excludes",
                table: "MembershipPlans",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IncludesGymAccess",
                table: "MembershipPlans",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IncludesInbody",
                table: "MembershipPlans",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "LoyaltyDiscount",
                table: "MembershipPlans",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Excludes",
                table: "MembershipPlans");

            migrationBuilder.DropColumn(
                name: "IncludesGymAccess",
                table: "MembershipPlans");

            migrationBuilder.DropColumn(
                name: "IncludesInbody",
                table: "MembershipPlans");

            migrationBuilder.DropColumn(
                name: "LoyaltyDiscount",
                table: "MembershipPlans");

            migrationBuilder.AlterColumn<string>(
                name: "Benefits",
                table: "MembershipPlans",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);
        }
    }
}
