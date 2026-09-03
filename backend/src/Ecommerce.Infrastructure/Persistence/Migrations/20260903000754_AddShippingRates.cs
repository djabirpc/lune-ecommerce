using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Ecommerce.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddShippingRates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ShippingRates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Wilaya = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    HomeDeliveryPrice = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    StopDeskPrice = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShippingRates", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "ShippingRates",
                columns: new[] { "Id", "CreatedAtUtc", "HomeDeliveryPrice", "IsActive", "StopDeskPrice", "UpdatedAtUtc", "Wilaya" },
                values: new object[,]
                {
                    { new Guid("0965f7ad-f55c-ddb5-aa40-99f183ff7283"), new DateTime(2026, 9, 3, 0, 0, 0, 0, DateTimeKind.Utc), 600m, true, 400m, null, "In Salah" },
                    { new Guid("0b12b277-5267-497a-c867-3e2e27b9d237"), new DateTime(2026, 9, 3, 0, 0, 0, 0, DateTimeKind.Utc), 600m, true, 400m, null, "Mascara" },
                    { new Guid("10246243-6b88-61bf-d376-059d33a9ba3d"), new DateTime(2026, 9, 3, 0, 0, 0, 0, DateTimeKind.Utc), 600m, true, 400m, null, "Djelfa" },
                    { new Guid("10740bdb-174b-d17f-2572-20d51bbc0cdd"), new DateTime(2026, 9, 3, 0, 0, 0, 0, DateTimeKind.Utc), 600m, true, 400m, null, "Aïn Témouchent" },
                    { new Guid("17ca516d-8d51-016d-9bb7-54ea2d8627d2"), new DateTime(2026, 9, 3, 0, 0, 0, 0, DateTimeKind.Utc), 600m, true, 400m, null, "Annaba" },
                    { new Guid("1aa5ffe2-4afa-197f-76e2-34dea51eea1d"), new DateTime(2026, 9, 3, 0, 0, 0, 0, DateTimeKind.Utc), 600m, true, 400m, null, "Sidi Bel Abbès" },
                    { new Guid("1af4ec57-c924-e1c3-ab77-80e9cfa901bc"), new DateTime(2026, 9, 3, 0, 0, 0, 0, DateTimeKind.Utc), 600m, true, 400m, null, "Ouargla" },
                    { new Guid("1d8ce0c6-7f83-a733-54ef-0289a4712e0e"), new DateTime(2026, 9, 3, 0, 0, 0, 0, DateTimeKind.Utc), 600m, true, 400m, null, "Béni Abbès" },
                    { new Guid("24dbf559-0432-7241-a377-157209af87f1"), new DateTime(2026, 9, 3, 0, 0, 0, 0, DateTimeKind.Utc), 600m, true, 400m, null, "Khenchela" },
                    { new Guid("26f849ae-e588-74ff-ddbe-622bc15f9028"), new DateTime(2026, 9, 3, 0, 0, 0, 0, DateTimeKind.Utc), 600m, true, 400m, null, "Médéa" },
                    { new Guid("274740bf-7e89-2b24-1238-485559ed5874"), new DateTime(2026, 9, 3, 0, 0, 0, 0, DateTimeKind.Utc), 600m, true, 400m, null, "Oum El Bouaghi" },
                    { new Guid("29263efe-2c2d-b191-8ff8-798693252b86"), new DateTime(2026, 9, 3, 0, 0, 0, 0, DateTimeKind.Utc), 600m, true, 400m, null, "Tamanrasset" },
                    { new Guid("2a452349-7f6f-30d1-9ed2-37edc02dff34"), new DateTime(2026, 9, 3, 0, 0, 0, 0, DateTimeKind.Utc), 600m, true, 400m, null, "Mostaganem" },
                    { new Guid("2c7ff4d0-24ae-4f16-cbe9-5109393f8826"), new DateTime(2026, 9, 3, 0, 0, 0, 0, DateTimeKind.Utc), 600m, true, 400m, null, "Tiaret" },
                    { new Guid("2e377d7b-d397-ff6c-adec-a61848d8e7bc"), new DateTime(2026, 9, 3, 0, 0, 0, 0, DateTimeKind.Utc), 600m, true, 400m, null, "Bordj Badji Mokhtar" },
                    { new Guid("438388b0-45de-bbb9-9f52-4aa01d293c95"), new DateTime(2026, 9, 3, 0, 0, 0, 0, DateTimeKind.Utc), 600m, true, 400m, null, "Ouled Djellal" },
                    { new Guid("4782964d-4241-ea37-ab11-af4ca63389d5"), new DateTime(2026, 9, 3, 0, 0, 0, 0, DateTimeKind.Utc), 600m, true, 400m, null, "In Guezzam" },
                    { new Guid("4dbc614d-7a52-603e-a93d-bad4b483f3b4"), new DateTime(2026, 9, 3, 0, 0, 0, 0, DateTimeKind.Utc), 600m, true, 400m, null, "Laghouat" },
                    { new Guid("4fafe79a-cc05-b010-3b08-55ded4857beb"), new DateTime(2026, 9, 3, 0, 0, 0, 0, DateTimeKind.Utc), 600m, true, 400m, null, "El Bayadh" },
                    { new Guid("50d0b27a-5474-c860-8773-b3c161a8ad83"), new DateTime(2026, 9, 3, 0, 0, 0, 0, DateTimeKind.Utc), 600m, true, 400m, null, "Tébessa" },
                    { new Guid("5274b4da-50a1-c9d1-ac63-8cbbb4a9e80a"), new DateTime(2026, 9, 3, 0, 0, 0, 0, DateTimeKind.Utc), 600m, true, 400m, null, "Boumerdès" },
                    { new Guid("55e9b1e9-e638-1455-92da-bd8940abdfea"), new DateTime(2026, 9, 3, 0, 0, 0, 0, DateTimeKind.Utc), 600m, true, 400m, null, "Béchar" },
                    { new Guid("594d2d21-c3f7-d68f-e6bb-94a56185fa3b"), new DateTime(2026, 9, 3, 0, 0, 0, 0, DateTimeKind.Utc), 600m, true, 400m, null, "El Oued" },
                    { new Guid("596ef71e-e2e0-1e1f-1ccd-ad11810230b3"), new DateTime(2026, 9, 3, 0, 0, 0, 0, DateTimeKind.Utc), 600m, true, 400m, null, "Chlef" },
                    { new Guid("5ba72ccb-63bd-5795-254e-12fb3b7d4b15"), new DateTime(2026, 9, 3, 0, 0, 0, 0, DateTimeKind.Utc), 600m, true, 400m, null, "Bouira" },
                    { new Guid("5c5f0ab6-c692-b192-3576-17d53a88e64e"), new DateTime(2026, 9, 3, 0, 0, 0, 0, DateTimeKind.Utc), 600m, true, 400m, null, "El M'Ghair" },
                    { new Guid("5c61e69b-4dd2-4030-6c06-ae8256d82686"), new DateTime(2026, 9, 3, 0, 0, 0, 0, DateTimeKind.Utc), 600m, true, 400m, null, "Guelma" },
                    { new Guid("5e47453f-f3cc-eefa-1345-55a04658e4c8"), new DateTime(2026, 9, 3, 0, 0, 0, 0, DateTimeKind.Utc), 600m, true, 400m, null, "Tissemsilt" },
                    { new Guid("6283095c-1d1d-e455-1d44-2d1a42d77f4a"), new DateTime(2026, 9, 3, 0, 0, 0, 0, DateTimeKind.Utc), 600m, true, 400m, null, "Saïda" },
                    { new Guid("688da81d-3683-69dd-ad95-0fdfe9431775"), new DateTime(2026, 9, 3, 0, 0, 0, 0, DateTimeKind.Utc), 600m, true, 400m, null, "Constantine" },
                    { new Guid("7abef508-de0c-7af4-80b2-1ff73684938d"), new DateTime(2026, 9, 3, 0, 0, 0, 0, DateTimeKind.Utc), 600m, true, 400m, null, "Sétif" },
                    { new Guid("850fba4c-66f9-36fb-8b49-f77dea5a3aa3"), new DateTime(2026, 9, 3, 0, 0, 0, 0, DateTimeKind.Utc), 600m, true, 400m, null, "Adrar" },
                    { new Guid("8f63b149-1157-a82d-3119-2bdecbbe40a5"), new DateTime(2026, 9, 3, 0, 0, 0, 0, DateTimeKind.Utc), 600m, true, 400m, null, "El Tarf" },
                    { new Guid("91e40f32-a173-88a3-ad4f-b0bc89086838"), new DateTime(2026, 9, 3, 0, 0, 0, 0, DateTimeKind.Utc), 600m, true, 400m, null, "Illizi" },
                    { new Guid("92b7717b-2f87-b9ca-6f08-00ad4021b7d8"), new DateTime(2026, 9, 3, 0, 0, 0, 0, DateTimeKind.Utc), 600m, true, 400m, null, "Alger" },
                    { new Guid("95cf0d49-6db9-0041-8bb1-dacc6debafba"), new DateTime(2026, 9, 3, 0, 0, 0, 0, DateTimeKind.Utc), 600m, true, 400m, null, "Tlemcen" },
                    { new Guid("96e8a01a-524c-d1df-512f-d69bb535ce34"), new DateTime(2026, 9, 3, 0, 0, 0, 0, DateTimeKind.Utc), 600m, true, 400m, null, "Ghardaïa" },
                    { new Guid("99c1d2cb-8219-86d5-cd19-f828b2983ed1"), new DateTime(2026, 9, 3, 0, 0, 0, 0, DateTimeKind.Utc), 600m, true, 400m, null, "Skikda" },
                    { new Guid("9bf7b360-8e5b-9752-cd28-8ef314e7565e"), new DateTime(2026, 9, 3, 0, 0, 0, 0, DateTimeKind.Utc), 600m, true, 400m, null, "Touggourt" },
                    { new Guid("9f4a6791-6918-d0b5-9d38-866542ab0709"), new DateTime(2026, 9, 3, 0, 0, 0, 0, DateTimeKind.Utc), 600m, true, 400m, null, "Oran" },
                    { new Guid("ab2af003-1b94-e5af-a0a6-3c990aa8b756"), new DateTime(2026, 9, 3, 0, 0, 0, 0, DateTimeKind.Utc), 600m, true, 400m, null, "Blida" },
                    { new Guid("ae5ce39d-86f9-1f59-58a9-4b316e29b373"), new DateTime(2026, 9, 3, 0, 0, 0, 0, DateTimeKind.Utc), 600m, true, 400m, null, "Relizane" },
                    { new Guid("b73337ac-4e98-95cc-7bd5-e5a3bd0df7c3"), new DateTime(2026, 9, 3, 0, 0, 0, 0, DateTimeKind.Utc), 600m, true, 400m, null, "Tipaza" },
                    { new Guid("ca5fdc0c-cdb7-e646-380c-df4ccc019f7f"), new DateTime(2026, 9, 3, 0, 0, 0, 0, DateTimeKind.Utc), 600m, true, 400m, null, "El Meniaa" },
                    { new Guid("d067d54d-f1ff-b680-6abb-f23cd8dc8060"), new DateTime(2026, 9, 3, 0, 0, 0, 0, DateTimeKind.Utc), 600m, true, 400m, null, "Mila" },
                    { new Guid("d55bb122-8dd1-5d4e-01fc-ce3453f57f87"), new DateTime(2026, 9, 3, 0, 0, 0, 0, DateTimeKind.Utc), 600m, true, 400m, null, "Djanet" },
                    { new Guid("d5f78783-38b5-16e5-802f-b9e725bd5e6d"), new DateTime(2026, 9, 3, 0, 0, 0, 0, DateTimeKind.Utc), 600m, true, 400m, null, "Tizi Ouzou" },
                    { new Guid("d7fa96a2-d2e4-c73f-c1d6-47a3398d2580"), new DateTime(2026, 9, 3, 0, 0, 0, 0, DateTimeKind.Utc), 600m, true, 400m, null, "Naâma" },
                    { new Guid("e3fc76a4-592a-0c28-f300-a0ef05d0b4c9"), new DateTime(2026, 9, 3, 0, 0, 0, 0, DateTimeKind.Utc), 600m, true, 400m, null, "Souk Ahras" },
                    { new Guid("e7ecfee2-faec-0a84-6a9f-696c16361701"), new DateTime(2026, 9, 3, 0, 0, 0, 0, DateTimeKind.Utc), 600m, true, 400m, null, "Jijel" },
                    { new Guid("e8ed174b-be31-5e3f-171b-47a08e858833"), new DateTime(2026, 9, 3, 0, 0, 0, 0, DateTimeKind.Utc), 600m, true, 400m, null, "Tindouf" },
                    { new Guid("e9ff221b-ca2f-0058-02ce-c17bf2f6d548"), new DateTime(2026, 9, 3, 0, 0, 0, 0, DateTimeKind.Utc), 600m, true, 400m, null, "Batna" },
                    { new Guid("eba2a9a3-8514-9651-940e-93957a5d20aa"), new DateTime(2026, 9, 3, 0, 0, 0, 0, DateTimeKind.Utc), 600m, true, 400m, null, "Bordj Bou Arréridj" },
                    { new Guid("ebdd2f90-7106-f706-b880-44532f03adcc"), new DateTime(2026, 9, 3, 0, 0, 0, 0, DateTimeKind.Utc), 600m, true, 400m, null, "Biskra" },
                    { new Guid("f0aeebde-548b-276d-4a28-6c552eec8bbf"), new DateTime(2026, 9, 3, 0, 0, 0, 0, DateTimeKind.Utc), 600m, true, 400m, null, "Timimoun" },
                    { new Guid("f2e73d67-7f73-d62d-002d-4b789fb23bfc"), new DateTime(2026, 9, 3, 0, 0, 0, 0, DateTimeKind.Utc), 600m, true, 400m, null, "Béjaïa" },
                    { new Guid("f84f9b05-f31a-f56a-ef03-4cd7361a7bf8"), new DateTime(2026, 9, 3, 0, 0, 0, 0, DateTimeKind.Utc), 600m, true, 400m, null, "M'Sila" },
                    { new Guid("fa6ce75d-9d9a-5ef0-1bac-868252b138a0"), new DateTime(2026, 9, 3, 0, 0, 0, 0, DateTimeKind.Utc), 600m, true, 400m, null, "Aïn Defla" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ShippingRates_Wilaya",
                table: "ShippingRates",
                column: "Wilaya",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ShippingRates");
        }
    }
}
