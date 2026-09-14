using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ecommerce.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPromotionBundleTiers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BundleQuantity",
                table: "Promotions");

            migrationBuilder.DropColumn(
                name: "BundleTotalPrice",
                table: "Promotions");

            migrationBuilder.DropColumn(
                name: "IncludesFreeShipping",
                table: "Promotions");

            migrationBuilder.CreateTable(
                name: "PromotionBundleTiers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PromotionId = table.Column<Guid>(type: "uuid", nullable: false),
                    BundleQuantity = table.Column<int>(type: "integer", nullable: false),
                    BundleTotalPrice = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    IncludesFreeShipping = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PromotionBundleTiers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PromotionBundleTiers_Promotions_PromotionId",
                        column: x => x.PromotionId,
                        principalTable: "Promotions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PromotionBundleTiers_PromotionId",
                table: "PromotionBundleTiers",
                column: "PromotionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PromotionBundleTiers");

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

            migrationBuilder.AddColumn<bool>(
                name: "IncludesFreeShipping",
                table: "Promotions",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
