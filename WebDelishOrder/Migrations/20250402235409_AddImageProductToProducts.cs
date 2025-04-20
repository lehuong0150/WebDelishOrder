using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebDelishOrder.Migrations
{
    /// <inheritdoc />
    public partial class AddImageProductToProducts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImageProduct",
                table: "products",
                type: "longtext",
                nullable: true,
                collation: "utf8mb4_0900_ai_ci")
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageProduct",
                table: "products");
        }
    }
}
