using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LearningEdge.Infrastructure.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class ExtractAndMCQIntiationFieldsToTraining : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsFileExtractionRequested",
                schema: "le",
                table: "TrainingFile",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsMCQGenerationRequested",
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
                name: "IsFileExtractionRequested",
                schema: "le",
                table: "TrainingFile");

            migrationBuilder.DropColumn(
                name: "IsMCQGenerationRequested",
                schema: "le",
                table: "TrainingFile");
        }
    }
}
