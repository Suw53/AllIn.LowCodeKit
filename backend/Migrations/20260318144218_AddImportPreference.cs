using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AllIn.LowCodeKit.Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddImportPreference : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ImportPreferences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MenuId = table.Column<int>(type: "INTEGER", nullable: false),
                    UseMappingEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    LastMappingConfigId = table.Column<int>(type: "INTEGER", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportPreferences", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ImportPreferences");
        }
    }
}
