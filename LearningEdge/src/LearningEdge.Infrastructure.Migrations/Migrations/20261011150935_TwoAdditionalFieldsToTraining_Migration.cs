using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LearningEdge.Infrastructure.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class TwoAdditionalFieldsToTraining_Migration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsFileExtractionStarted",
                schema: "le",
                table: "TrainingFile",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsMCQGenerationStarted",
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
                name: "IsFileExtractionStarted",
                schema: "le",
                table: "TrainingFile");

            migrationBuilder.DropColumn(
                name: "IsMCQGenerationStarted",
                schema: "le",
                table: "TrainingFile");
        }
    }
}
