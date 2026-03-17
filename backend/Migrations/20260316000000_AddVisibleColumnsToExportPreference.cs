using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AllIn.LowCodeKit.Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddVisibleColumnsToExportPreference : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "VisibleColumns",
                table: "ExportPreferences",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VisibleColumns",
                table: "ExportPreferences");
        }
    }
}
