using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Codino.Api.Migrations
{
    /// <inheritdoc />
    public partial class UpdateCodinoDbContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.AddColumn<bool>(
                name: "IsFree",
                table: "CourseSessions",
                type: "bit",
                nullable: false,
                defaultValue: false);

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

            migrationBuilder.AddColumn<bool>(
                name: "IsFree",
                table: "CourseChapters",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_QuizSubmissions_CourseSessionId1",
                table: "QuizSubmissions",
                column: "CourseSessionId1");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
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
                name: "IsFree",
                table: "CourseSessions");

            migrationBuilder.DropColumn(
                name: "CourseSessionId1",
                table: "CourseSessionProgresses");

            migrationBuilder.DropColumn(
                name: "UserId1",
                table: "CourseSessionProgresses");

            migrationBuilder.DropColumn(
                name: "IsFree",
                table: "CourseChapters");
        }
    }
}
