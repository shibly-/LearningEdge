using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LearningEdge.Infrastructure.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class ErrorDetailsFieldsToTraining : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FileExtractionFailureMessage",
                schema: "le",
                table: "TrainingFile",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsFileExtractionFailed",
                schema: "le",
                table: "TrainingFile",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsMCQGenerationFailed",
                schema: "le",
                table: "TrainingFile",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "MCQGenerationFailureMessage",
                schema: "le",
                table: "TrainingFile",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FileExtractionFailureMessage",
                schema: "le",
                table: "TrainingFile");

            migrationBuilder.DropColumn(
                name: "IsFileExtractionFailed",
                schema: "le",
                table: "TrainingFile");

            migrationBuilder.DropColumn(
                name: "IsMCQGenerationFailed",
                schema: "le",
                table: "TrainingFile");

            migrationBuilder.DropColumn(
                name: "MCQGenerationFailureMessage",
                schema: "le",
                table: "TrainingFile");
        }
    }
}
