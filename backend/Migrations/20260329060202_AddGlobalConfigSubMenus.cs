using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace AllIn.LowCodeKit.Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddGlobalConfigSubMenus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Menus",
                columns: new[] { "Id", "CreatedAt", "Icon", "IsSystem", "Name", "ParentId", "Sort" },
                values: new object[,]
                {
                    { 2, new DateTime(2026, 3, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), "User", true, "登录配置", 1, 1 },
                    { 3, new DateTime(2026, 3, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), "Monitor", true, "Playwright", 1, 2 },
                    { 4, new DateTime(2026, 3, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), "Brush", true, "个性化", 1, 3 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Menus",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Menus",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Menus",
                keyColumn: "Id",
                keyValue: 4);
        }
    }
}
