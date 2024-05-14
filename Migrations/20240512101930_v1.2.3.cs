using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace XEDAPVIP.Migrations
{
    /// <inheritdoc />
    public partial class v123 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Product_Slug",
                table: "Product");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "ProductDetail");

            migrationBuilder.RenameColumn(
                name: "Value",
                table: "ProductDetail",
                newName: "Details");

            migrationBuilder.AlterColumn<string>(
                name: "Slug",
                table: "Product",
                type: "nvarchar(160)",
                maxLength: 160,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(160)",
                oldMaxLength: 160);

            migrationBuilder.CreateIndex(
                name: "IX_Product_Slug",
                table: "Product",
                column: "Slug",
                unique: true,
                filter: "[Slug] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Product_Slug",
                table: "Product");

            migrationBuilder.RenameColumn(
                name: "Details",
                table: "ProductDetail",
                newName: "Value");

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "ProductDetail",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "Slug",
                table: "Product",
                type: "nvarchar(160)",
                maxLength: 160,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(160)",
                oldMaxLength: 160,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Product_Slug",
                table: "Product",
                column: "Slug",
                unique: true);
        }
    }
}
