using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace XEDAPVIP.Migrations
{
    /// <inheritdoc />
    public partial class v126 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ProductDetailId",
                table: "Product",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProductDetailId",
                table: "Product");
        }
    }
}
