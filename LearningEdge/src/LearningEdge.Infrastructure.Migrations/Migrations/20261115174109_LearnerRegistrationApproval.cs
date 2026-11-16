using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LearningEdge.Infrastructure.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class LearnerRegistrationApproval : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ActivationStatus",
                schema: "le",
                table: "User",
                type: "int",
                nullable: false,
                defaultValue: 2);

            migrationBuilder.AddColumn<string>(
                name: "KeycloakUserId",
                schema: "le",
                table: "User",
                type: "nvarchar(36)",
                maxLength: 36,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PendingPasswordProtected",
                schema: "le",
                table: "User",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActivationStatus",
                schema: "le",
                table: "User");

            migrationBuilder.DropColumn(
                name: "KeycloakUserId",
                schema: "le",
                table: "User");

            migrationBuilder.DropColumn(
                name: "PendingPasswordProtected",
                schema: "le",
                table: "User");
        }
    }
}
