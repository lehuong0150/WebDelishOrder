using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebDelishOrder.Migrations
{
    /// <inheritdoc />
    public partial class RemoveProductImage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "product_images");

            migrationBuilder.AlterColumn<bool>(
                name: "is_available",
                table: "products",
                type: "tinyint(1)",
                nullable: false,
                defaultValueSql: "'1'",
                oldClrType: typeof(bool),
                oldType: "tinyint(1)",
                oldNullable: true,
                oldDefaultValueSql: "'1'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<bool>(
                name: "is_available",
                table: "products",
                type: "tinyint(1)",
                nullable: true,
                defaultValueSql: "'1'",
                oldClrType: typeof(bool),
                oldType: "tinyint(1)",
                oldDefaultValueSql: "'1'");

            migrationBuilder.CreateTable(
                name: "product_images",
                columns: table => new
                {
                    product_id = table.Column<int>(type: "int", nullable: false),
                    image = table.Column<string>(type: "varchar(255)", nullable: false, collation: "utf8mb3_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb3")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.product_id, x.image })
                        .Annotation("MySql:IndexPrefixLength", new[] { 0, 0 });
                    table.ForeignKey(
                        name: "product_images_ibfk_1",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_0900_ai_ci");
        }
    }
}
