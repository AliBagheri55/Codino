
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Codino.Api.Migrations
{
    public partial class AddQuizOptionsAndTestCases : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // QuizOptions و QuizTestCases از قبل در دیتابیس وجود دارند.
            // در این مهاجرت هیچ جدولی ساخته یا حذف نمی‌شود.
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // عمداً خالی است تا جدول‌های موجود حذف نشوند.
        }
    }
}
