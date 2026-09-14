using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ecommerce.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderNegotiationAndCoupon : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CouponCode",
                table: "Orders",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ManualDiscountAmount",
                table: "Orders",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "NegotiatedFreeShipping",
                table: "Orders",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CouponCode",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ManualDiscountAmount",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "NegotiatedFreeShipping",
                table: "Orders");
        }
    }
}
