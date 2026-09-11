using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ecommerce.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBundlePricePromotion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BundleQuantity",
                table: "Promotions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "BundleTotalPrice",
                table: "Promotions",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BundleQuantity",
                table: "Promotions");

            migrationBuilder.DropColumn(
                name: "BundleTotalPrice",
                table: "Promotions");
        }
    }
}
