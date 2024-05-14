using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace XEDAPVIP.Migrations
{
    /// <inheritdoc />
    public partial class v12 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductCategory_Category_CategoryID",
                table: "ProductCategory");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductCategory_Product_ProductID",
                table: "ProductCategory");

            migrationBuilder.RenameColumn(
                name: "CategoryID",
                table: "ProductCategory",
                newName: "CategoryId");

            migrationBuilder.RenameColumn(
                name: "ProductID",
                table: "ProductCategory",
                newName: "ProductId");

            migrationBuilder.RenameIndex(
                name: "IX_ProductCategory_CategoryID",
                table: "ProductCategory",
                newName: "IX_ProductCategory_CategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductCategory_Category_CategoryId",
                table: "ProductCategory",
                column: "CategoryId",
                principalTable: "Category",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductCategory_Product_ProductId",
                table: "ProductCategory",
                column: "ProductId",
                principalTable: "Product",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductCategory_Category_CategoryId",
                table: "ProductCategory");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductCategory_Product_ProductId",
                table: "ProductCategory");

            migrationBuilder.RenameColumn(
                name: "CategoryId",
                table: "ProductCategory",
                newName: "CategoryID");

            migrationBuilder.RenameColumn(
                name: "ProductId",
                table: "ProductCategory",
                newName: "ProductID");

            migrationBuilder.RenameIndex(
                name: "IX_ProductCategory_CategoryId",
                table: "ProductCategory",
                newName: "IX_ProductCategory_CategoryID");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductCategory_Category_CategoryID",
                table: "ProductCategory",
                column: "CategoryID",
                principalTable: "Category",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductCategory_Product_ProductID",
                table: "ProductCategory",
                column: "ProductID",
                principalTable: "Product",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
