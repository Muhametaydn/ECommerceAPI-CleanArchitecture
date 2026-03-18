using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ECommerce.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "Description", "Name", "ParentCategoryId", "Slug", "UpdateAt", "UpdatedBy" },
                values: new object[,]
                {
                    { new Guid("a1111111-1111-1111-1111-111111111111"), new DateTime(2026, 3, 18, 2, 3, 23, 718, DateTimeKind.Utc).AddTicks(1863), null, "Elektronik ürünler", "Elektronik", null, "elektronik", null, null },
                    { new Guid("b2222222-2222-2222-2222-222222222222"), new DateTime(2026, 3, 18, 2, 3, 23, 718, DateTimeKind.Utc).AddTicks(1865), null, "Giyim ürünleri", "Giyim", null, "giyim", null, null },
                    { new Guid("c3333333-3333-3333-3333-333333333333"), new DateTime(2026, 3, 18, 2, 3, 23, 718, DateTimeKind.Utc).AddTicks(1867), null, "Ev ve yaşam ürünleri", "Ev & Yaşam", null, "ev-yasam", null, null }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("a1111111-1111-1111-1111-111111111111"));

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("b2222222-2222-2222-2222-222222222222"));

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("c3333333-3333-3333-3333-333333333333"));
        }
    }
}
