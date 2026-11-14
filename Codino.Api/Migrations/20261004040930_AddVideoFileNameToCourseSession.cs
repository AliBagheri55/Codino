using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Codino.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddVideoFileNameToCourseSession : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "VideoFileName",
                table: "CourseSessions",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CourseSessions_CourseChapterId_IsActive",
                table: "CourseSessions",
                columns: new[] { "CourseChapterId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_CourseSessions_VideoFileName",
                table: "CourseSessions",
                column: "VideoFileName");

            migrationBuilder.CreateIndex(
                name: "IX_Courses_Id_IsActive",
                table: "Courses",
                columns: new[] { "Id", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_CourseChapters_CourseId_IsActive",
                table: "CourseChapters",
                columns: new[] { "CourseId", "IsActive" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CourseSessions_CourseChapterId_IsActive",
                table: "CourseSessions");

            migrationBuilder.DropIndex(
                name: "IX_CourseSessions_VideoFileName",
                table: "CourseSessions");

            migrationBuilder.DropIndex(
                name: "IX_Courses_Id_IsActive",
                table: "Courses");

            migrationBuilder.DropIndex(
                name: "IX_CourseChapters_CourseId_IsActive",
                table: "CourseChapters");

            migrationBuilder.DropColumn(
                name: "VideoFileName",
                table: "CourseSessions");
        }
    }
}
