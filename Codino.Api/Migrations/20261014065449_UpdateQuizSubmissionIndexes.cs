using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Codino.Api.Migrations
{
    /// <inheritdoc />
    public partial class UpdateQuizSubmissionIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CourseSessionProgresses_CourseSessions_CourseSessionId1",
                table: "CourseSessionProgresses");

            migrationBuilder.DropForeignKey(
                name: "FK_CourseSessionProgresses_Users_UserId1",
                table: "CourseSessionProgresses");

            migrationBuilder.DropForeignKey(
                name: "FK_QuizSubmissions_CourseSessions_CourseSessionId1",
                table: "QuizSubmissions");

            migrationBuilder.DropForeignKey(
                name: "FK_QuizSubmissions_Users_UserId1",
                table: "QuizSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_QuizSubmissions_CourseSessionId1",
                table: "QuizSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_QuizSubmissions_UserId_SubmissionId",
                table: "QuizSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_QuizSubmissions_UserId1",
                table: "QuizSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_CourseSessionProgresses_CourseSessionId1",
                table: "CourseSessionProgresses");

            migrationBuilder.DropIndex(
                name: "IX_CourseSessionProgresses_UserId1",
                table: "CourseSessionProgresses");

            migrationBuilder.DropColumn(
                name: "CourseSessionId1",
                table: "QuizSubmissions");

            migrationBuilder.DropColumn(
                name: "UserId1",
                table: "QuizSubmissions");

            migrationBuilder.DropColumn(
                name: "CourseSessionId1",
                table: "CourseSessionProgresses");

            migrationBuilder.DropColumn(
                name: "UserId1",
                table: "CourseSessionProgresses");

            migrationBuilder.AlterColumn<bool>(
                name: "IsFree",
                table: "CourseChapters",
                type: "bit",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.CreateIndex(
                name: "IX_QuizSubmissions_UserId_CourseSessionId_SubmissionId",
                table: "QuizSubmissions",
                columns: new[] { "UserId", "CourseSessionId", "SubmissionId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_QuizSubmissions_UserId_CourseSessionId_SubmissionId",
                table: "QuizSubmissions");

            migrationBuilder.AddColumn<int>(
                name: "CourseSessionId1",
                table: "QuizSubmissions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UserId1",
                table: "QuizSubmissions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CourseSessionId1",
                table: "CourseSessionProgresses",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UserId1",
                table: "CourseSessionProgresses",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "IsFree",
                table: "CourseChapters",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_QuizSubmissions_CourseSessionId1",
                table: "QuizSubmissions",
                column: "CourseSessionId1");

            migrationBuilder.CreateIndex(
                name: "IX_QuizSubmissions_UserId_SubmissionId",
                table: "QuizSubmissions",
                columns: new[] { "UserId", "SubmissionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QuizSubmissions_UserId1",
                table: "QuizSubmissions",
                column: "UserId1");

            migrationBuilder.CreateIndex(
                name: "IX_CourseSessionProgresses_CourseSessionId1",
                table: "CourseSessionProgresses",
                column: "CourseSessionId1");

            migrationBuilder.CreateIndex(
                name: "IX_CourseSessionProgresses_UserId1",
                table: "CourseSessionProgresses",
                column: "UserId1");

            migrationBuilder.AddForeignKey(
                name: "FK_CourseSessionProgresses_CourseSessions_CourseSessionId1",
                table: "CourseSessionProgresses",
                column: "CourseSessionId1",
                principalTable: "CourseSessions",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CourseSessionProgresses_Users_UserId1",
                table: "CourseSessionProgresses",
                column: "UserId1",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_QuizSubmissions_CourseSessions_CourseSessionId1",
                table: "QuizSubmissions",
                column: "CourseSessionId1",
                principalTable: "CourseSessions",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_QuizSubmissions_Users_UserId1",
                table: "QuizSubmissions",
                column: "UserId1",
                principalTable: "Users",
                principalColumn: "Id");
        }
    }
}
