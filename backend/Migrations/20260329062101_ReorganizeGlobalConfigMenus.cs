using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace AllIn.LowCodeKit.Backend.Migrations
{
    /// <inheritdoc />
    public partial class ReorganizeGlobalConfigMenus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 先插入新的二级菜单
            migrationBuilder.InsertData(
                table: "Menus",
                columns: new[] { "Id", "CreatedAt", "Icon", "IsSystem", "Name", "ParentId", "Sort" },
                values: new object[,]
                {
                    { 100, new DateTime(2026, 3, 29, 0, 0, 0, 0, DateTimeKind.Unspecified), "Connection", true, "自动化配置", 1, 1 },
                    { 101, new DateTime(2026, 3, 29, 0, 0, 0, 0, DateTimeKind.Unspecified), "Tools", true, "系统配置", 1, 2 }
                });

            // 插入新的三级菜单
            migrationBuilder.InsertData(
                table: "Menus",
                columns: new[] { "Id", "CreatedAt", "Icon", "IsSystem", "Name", "ParentId", "Sort" },
                values: new object[,]
                {
                    { 102, new DateTime(2026, 3, 29, 0, 0, 0, 0, DateTimeKind.Unspecified), "User", true, "登录配置", 100, 1 },
                    { 103, new DateTime(2026, 3, 29, 0, 0, 0, 0, DateTimeKind.Unspecified), "Monitor", true, "Playwright", 100, 2 },
                    { 104, new DateTime(2026, 3, 29, 0, 0, 0, 0, DateTimeKind.Unspecified), "Brush", true, "个性化", 101, 1 }
                });

            // 删除旧的二级菜单（如果存在且没有关联数据）
            migrationBuilder.Sql(@"
                DELETE FROM Menus WHERE Id = 2 AND IsSystem = 1;
                DELETE FROM Menus WHERE Id = 3 AND IsSystem = 1;
                DELETE FROM Menus WHERE Id = 4 AND IsSystem = 1;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Menus",
                keyColumn: "Id",
                keyValue: 102);

            migrationBuilder.DeleteData(
                table: "Menus",
                keyColumn: "Id",
                keyValue: 103);

            migrationBuilder.DeleteData(
                table: "Menus",
                keyColumn: "Id",
                keyValue: 104);

            migrationBuilder.DeleteData(
                table: "Menus",
                keyColumn: "Id",
                keyValue: 100);

            migrationBuilder.DeleteData(
                table: "Menus",
                keyColumn: "Id",
                keyValue: 101);

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
    }
}
