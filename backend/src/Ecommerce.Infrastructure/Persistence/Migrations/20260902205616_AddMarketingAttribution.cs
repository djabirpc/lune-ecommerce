using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ecommerce.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMarketingAttribution : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Fbclid",
                table: "Orders",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LandingPage",
                table: "Orders",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Referrer",
                table: "Orders",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Ttclid",
                table: "Orders",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UtmCampaign",
                table: "Orders",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UtmContent",
                table: "Orders",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UtmMedium",
                table: "Orders",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UtmSource",
                table: "Orders",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UtmTerm",
                table: "Orders",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_UtmSource",
                table: "Orders",
                column: "UtmSource");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Orders_UtmSource",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "Fbclid",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "LandingPage",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "Referrer",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "Ttclid",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "UtmCampaign",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "UtmContent",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "UtmMedium",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "UtmSource",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "UtmTerm",
                table: "Orders");
        }
    }
}
