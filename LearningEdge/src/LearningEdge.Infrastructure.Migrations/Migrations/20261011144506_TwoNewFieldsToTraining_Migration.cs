using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LearningEdge.Infrastructure.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class TwoNewFieldsToTraining_Migration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsFileExtracted",
                schema: "le",
                table: "TrainingFile",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsMCQGenerated",
                schema: "le",
                table: "TrainingFile",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsFileExtracted",
                schema: "le",
                table: "TrainingFile");

            migrationBuilder.DropColumn(
                name: "IsMCQGenerated",
                schema: "le",
                table: "TrainingFile");
        }
    }
}
