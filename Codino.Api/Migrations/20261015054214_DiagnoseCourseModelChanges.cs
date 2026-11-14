
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Codino.Api.Migrations
{
    public partial class DiagnoseCourseModelChanges : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // اصلاح نام ایندکس یکتای QuizOptions
            migrationBuilder.DropIndex(
                name: "IX_QuizOptions_Question_Order",
                table: "QuizOptions");

            migrationBuilder.CreateIndex(
                name: "IX_QuizOptions_CourseSessionQuestionId_Order",
                table: "QuizOptions",
                columns: new[]
                {
                    "CourseSessionQuestionId",
                    "Order"
                },
                unique: true);

            // اصلاح ایندکس QuizTestCases
            migrationBuilder.DropIndex(
                name: "IX_QuizTestCases_QuestionId",
                table: "QuizTestCases");

            migrationBuilder.CreateIndex(
                name: "IX_QuizTestCases_CourseSessionQuestionId_IsSample",
                table: "QuizTestCases",
                columns: new[]
                {
                    "CourseSessionQuestionId",
                    "IsSample"
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_QuizOptions_CourseSessionQuestionId_Order",
                table: "QuizOptions");

            migrationBuilder.CreateIndex(
                name: "IX_QuizOptions_Question_Order",
                table: "QuizOptions",
                columns: new[]
                {
                    "CourseSessionQuestionId",
                    "Order"
                },
                unique: true);

            migrationBuilder.DropIndex(
                name: "IX_QuizTestCases_CourseSessionQuestionId_IsSample",
                table: "QuizTestCases");

            migrationBuilder.CreateIndex(
                name: "IX_QuizTestCases_QuestionId",
                table: "QuizTestCases",
                column: "CourseSessionQuestionId");
        }
    }
}
