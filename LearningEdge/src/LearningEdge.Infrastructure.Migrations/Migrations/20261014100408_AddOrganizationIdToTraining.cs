using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LearningEdge.Infrastructure.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationIdToTraining : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                schema: "le",
                table: "Training",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE training
                SET training.OrganizationId = category.OrganizationId
                FROM [le].[Training] AS training
                INNER JOIN [le].[Category] AS category ON training.CategoryId = category.Id
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "OrganizationId",
                schema: "le",
                table: "Training",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Training_OrganizationId",
                schema: "le",
                table: "Training",
                column: "OrganizationId");

            migrationBuilder.AddForeignKey(
                name: "FK_Training_Organization_OrganizationId",
                schema: "le",
                table: "Training",
                column: "OrganizationId",
                principalSchema: "le",
                principalTable: "Organization",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Training_Organization_OrganizationId",
                schema: "le",
                table: "Training");

            migrationBuilder.DropIndex(
                name: "IX_Training_OrganizationId",
                schema: "le",
                table: "Training");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                schema: "le",
                table: "Training");
        }
    }
}
