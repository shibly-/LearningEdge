using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LearningEdge.Infrastructure.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddQuestionBanksExamsAndPromptRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Exam",
                schema: "le",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TrainingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PreparedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Exam", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Exam_Training_TrainingId",
                        column: x => x.TrainingId,
                        principalSchema: "le",
                        principalTable: "Training",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Exam_User_PreparedByUserId",
                        column: x => x.PreparedByUserId,
                        principalSchema: "le",
                        principalTable: "User",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "QuestionBank",
                schema: "le",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TrainingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestionBank", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuestionBank_Training_TrainingId",
                        column: x => x.TrainingId,
                        principalSchema: "le",
                        principalTable: "Training",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PromptRequest",
                schema: "le",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TrainingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuestionBankId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PromptText = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    QuestionType = table.Column<int>(type: "int", nullable: false),
                    QuestionCount = table.Column<int>(type: "int", nullable: false),
                    RequestedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PromptRequest", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PromptRequest_QuestionBank_QuestionBankId",
                        column: x => x.QuestionBankId,
                        principalSchema: "le",
                        principalTable: "QuestionBank",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PromptRequest_Training_TrainingId",
                        column: x => x.TrainingId,
                        principalSchema: "le",
                        principalTable: "Training",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Question",
                schema: "le",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Text = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    QuestionBankId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Score = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Question", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Question_QuestionBank_QuestionBankId",
                        column: x => x.QuestionBankId,
                        principalSchema: "le",
                        principalTable: "QuestionBank",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExamQuestion",
                schema: "le",
                columns: table => new
                {
                    ExamId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuestionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamQuestion", x => new { x.ExamId, x.QuestionId });
                    table.ForeignKey(
                        name: "FK_ExamQuestion_Exam_ExamId",
                        column: x => x.ExamId,
                        principalSchema: "le",
                        principalTable: "Exam",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExamQuestion_Question_QuestionId",
                        column: x => x.QuestionId,
                        principalSchema: "le",
                        principalTable: "Question",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "McqQuestion",
                schema: "le",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Options = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Answer = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_McqQuestion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_McqQuestion_Question_Id",
                        column: x => x.Id,
                        principalSchema: "le",
                        principalTable: "Question",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MultipleSelectionQuestion",
                schema: "le",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Options = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Answer = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MultipleSelectionQuestion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MultipleSelectionQuestion_Question_Id",
                        column: x => x.Id,
                        principalSchema: "le",
                        principalTable: "Question",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TrueFalseQuestion",
                schema: "le",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Answer = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrueFalseQuestion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrueFalseQuestion_Question_Id",
                        column: x => x.Id,
                        principalSchema: "le",
                        principalTable: "Question",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Exam_PreparedByUserId",
                schema: "le",
                table: "Exam",
                column: "PreparedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Exam_TrainingId_Title",
                schema: "le",
                table: "Exam",
                columns: new[] { "TrainingId", "Title" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ExamQuestion_QuestionId",
                schema: "le",
                table: "ExamQuestion",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_PromptRequest_QuestionBankId",
                schema: "le",
                table: "PromptRequest",
                column: "QuestionBankId");

            migrationBuilder.CreateIndex(
                name: "IX_PromptRequest_TrainingId",
                schema: "le",
                table: "PromptRequest",
                column: "TrainingId");

            migrationBuilder.CreateIndex(
                name: "IX_Question_QuestionBankId",
                schema: "le",
                table: "Question",
                column: "QuestionBankId");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionBank_TrainingId_Title",
                schema: "le",
                table: "QuestionBank",
                columns: new[] { "TrainingId", "Title" },
                unique: true,
                filter: "[IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExamQuestion",
                schema: "le");

            migrationBuilder.DropTable(
                name: "McqQuestion",
                schema: "le");

            migrationBuilder.DropTable(
                name: "MultipleSelectionQuestion",
                schema: "le");

            migrationBuilder.DropTable(
                name: "PromptRequest",
                schema: "le");

            migrationBuilder.DropTable(
                name: "TrueFalseQuestion",
                schema: "le");

            migrationBuilder.DropTable(
                name: "Exam",
                schema: "le");

            migrationBuilder.DropTable(
                name: "Question",
                schema: "le");

            migrationBuilder.DropTable(
                name: "QuestionBank",
                schema: "le");
        }
    }
}
