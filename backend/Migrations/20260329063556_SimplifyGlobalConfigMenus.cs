using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace AllIn.LowCodeKit.Backend.Migrations
{
    /// <inheritdoc />
    public partial class SimplifyGlobalConfigMenus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 先更新子菜单的 ParentId
            migrationBuilder.UpdateData(
                table: "Menus",
                keyColumn: "Id",
                keyValue: 102,
                column: "ParentId",
                value: 1);

            migrationBuilder.UpdateData(
                table: "Menus",
                keyColumn: "Id",
                keyValue: 103,
                column: "ParentId",
                value: 1);

            migrationBuilder.UpdateData(
                table: "Menus",
                keyColumn: "Id",
                keyValue: 104,
                columns: new[] { "ParentId", "Sort" },
                values: new object[] { 1, 3 });

            // 再删除父菜单
            migrationBuilder.DeleteData(
                table: "Menus",
                keyColumn: "Id",
                keyValue: 100);

            migrationBuilder.DeleteData(
                table: "Menus",
                keyColumn: "Id",
                keyValue: 101);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Menus",
                keyColumn: "Id",
                keyValue: 102,
                column: "ParentId",
                value: 100);

            migrationBuilder.UpdateData(
                table: "Menus",
                keyColumn: "Id",
                keyValue: 103,
                column: "ParentId",
                value: 100);

            migrationBuilder.UpdateData(
                table: "Menus",
                keyColumn: "Id",
                keyValue: 104,
                columns: new[] { "ParentId", "Sort" },
                values: new object[] { 101, 1 });

            migrationBuilder.InsertData(
                table: "Menus",
                columns: new[] { "Id", "CreatedAt", "Icon", "IsSystem", "Name", "ParentId", "Sort" },
                values: new object[,]
                {
                    { 100, new DateTime(2026, 3, 29, 0, 0, 0, 0, DateTimeKind.Unspecified), "Connection", true, "自动化配置", 1, 1 },
                    { 101, new DateTime(2026, 3, 29, 0, 0, 0, 0, DateTimeKind.Unspecified), "Tools", true, "系统配置", 1, 2 }
                });
        }
    }
}
