using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AllIn.LowCodeKit.Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddFormFieldSpan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Span",
                table: "FormFields",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Span",
                table: "FormFields");
        }
    }
}
