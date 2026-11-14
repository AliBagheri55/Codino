using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Codino.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddHallOfFameFieldsToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CourseSessions_CourseChapterId_Order",
                table: "CourseSessions");

            migrationBuilder.DropIndex(
                name: "IX_Courses_Id_IsActive",
                table: "Courses");

            migrationBuilder.DropIndex(
                name: "IX_CourseChapters_CourseId_Order",
                table: "CourseChapters");

            migrationBuilder.AlterColumn<string>(
                name: "PhoneNumber",
                table: "Users",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "FullName",
                table: "Users",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "ProfileImageUrl",
                table: "Users",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ShowInHallOfFame",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "TotalPoints",
                table: "Users",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "VideoFileName",
                table: "CourseSessions",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_PhoneNumber",
                table: "Users",
                column: "PhoneNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_ShowInHallOfFame_TotalPoints",
                table: "Users",
                columns: new[] { "ShowInHallOfFame", "TotalPoints" });

            migrationBuilder.CreateIndex(
                name: "IX_CourseSessions_CourseChapterId_Order",
                table: "CourseSessions",
                columns: new[] { "CourseChapterId", "Order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Courses_IsActive",
                table: "Courses",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_CourseChapters_CourseId_Order",
                table: "CourseChapters",
                columns: new[] { "CourseId", "Order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Comments_IsApproved_CreatedAt",
                table: "Comments",
                columns: new[] { "IsApproved", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Comments_IsApproved_Rating",
                table: "Comments",
                columns: new[] { "IsApproved", "Rating" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Comments_Rating",
                table: "Comments",
                sql: "[Rating] >= 1 AND [Rating] <= 5");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_PhoneNumber",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_ShowInHallOfFame_TotalPoints",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_CourseSessions_CourseChapterId_Order",
                table: "CourseSessions");

            migrationBuilder.DropIndex(
                name: "IX_Courses_IsActive",
                table: "Courses");

            migrationBuilder.DropIndex(
                name: "IX_CourseChapters_CourseId_Order",
                table: "CourseChapters");

            migrationBuilder.DropIndex(
                name: "IX_Comments_IsApproved_CreatedAt",
                table: "Comments");

            migrationBuilder.DropIndex(
                name: "IX_Comments_IsApproved_Rating",
                table: "Comments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Comments_Rating",
                table: "Comments");

            migrationBuilder.DropColumn(
                name: "ProfileImageUrl",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ShowInHallOfFame",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "TotalPoints",
                table: "Users");

            migrationBuilder.AlterColumn<string>(
                name: "PhoneNumber",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "FullName",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "VideoFileName",
                table: "CourseSessions",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(255)",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CourseSessions_CourseChapterId_Order",
                table: "CourseSessions",
                columns: new[] { "CourseChapterId", "Order" });

            migrationBuilder.CreateIndex(
                name: "IX_Courses_Id_IsActive",
                table: "Courses",
                columns: new[] { "Id", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_CourseChapters_CourseId_Order",
                table: "CourseChapters",
                columns: new[] { "CourseId", "Order" });
        }
    }
}
