using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace XEDAPVIP.Migrations
{
    /// <inheritdoc />
    public partial class v1262 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProductDetailId",
                table: "Product");

            migrationBuilder.AddColumn<string>(
                name: "DetailsJson",
                table: "Product",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DetailsJson",
                table: "Product");

            migrationBuilder.AddColumn<int>(
                name: "ProductDetailId",
                table: "Product",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
