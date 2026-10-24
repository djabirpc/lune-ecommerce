using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ecommerce.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMarketingSettingsAndPurchaseEventTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "PurchaseEventSentAtUtc",
                table: "Orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MarketingSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PurchaseEventTrigger = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    MetaPixelId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    MetaAccessToken = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    TikTokPixelCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    TikTokAccessToken = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketingSettings", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MarketingSettings");

            migrationBuilder.DropColumn(
                name: "PurchaseEventSentAtUtc",
                table: "Orders");
        }
    }
}
