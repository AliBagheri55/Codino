using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Codino.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCourseEnrollmentPoints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Points",
                table: "CourseEnrollments",
                type: "int",
                nullable: false,
                defaultValue: 2000);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Points",
                table: "CourseEnrollments");
        }
    }
}
