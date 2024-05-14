using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace XEDAPVIP.Migrations
{
    /// <inheritdoc />
    public partial class v132 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MainImage",
                table: "Product",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SubImages",
                table: "Product",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "[]");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MainImage",
                table: "Product");

            migrationBuilder.DropColumn(
                name: "SubImages",
                table: "Product");
        }
    }
}
