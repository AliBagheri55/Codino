using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Codino.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddSecurityCodesToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // =========================================================
            // اضافه کردن SecurityCode1Hash به جدول Users
            // فقط در صورتی که این ستون از قبل وجود نداشته باشد
            // =========================================================

            migrationBuilder.Sql("""
                IF COL_LENGTH('dbo.Users', 'SecurityCode1Hash') IS NULL
                BEGIN
                    ALTER TABLE dbo.Users
                    ADD SecurityCode1Hash nvarchar(max) NOT NULL
                        CONSTRAINT DF_Users_SecurityCode1Hash DEFAULT '';
                END
                """);

            // =========================================================
            // اضافه کردن SecurityCode2Hash به جدول Users
            // فقط در صورتی که این ستون از قبل وجود نداشته باشد
            // =========================================================

            migrationBuilder.Sql("""
                IF COL_LENGTH('dbo.Users', 'SecurityCode2Hash') IS NULL
                BEGIN
                    ALTER TABLE dbo.Users
                    ADD SecurityCode2Hash nvarchar(max) NOT NULL
                        CONSTRAINT DF_Users_SecurityCode2Hash DEFAULT '';
                END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // =========================================================
            // حذف SecurityCode1Hash در صورت وجود
            // =========================================================

            migrationBuilder.Sql("""
                IF COL_LENGTH('dbo.Users', 'SecurityCode1Hash') IS NOT NULL
                BEGIN
                    ALTER TABLE dbo.Users
                    DROP COLUMN SecurityCode1Hash;
                END
                """);

            // =========================================================
            // حذف SecurityCode2Hash در صورت وجود
            // =========================================================

            migrationBuilder.Sql("""
                IF COL_LENGTH('dbo.Users', 'SecurityCode2Hash') IS NOT NULL
                BEGIN
                    ALTER TABLE dbo.Users
                    DROP COLUMN SecurityCode2Hash;
                END
                """);
        }
    }
}