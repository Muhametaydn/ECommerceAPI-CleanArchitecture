using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.Persistence.Migrations
{
    public partial class SeedProducts : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Products",
                columns: new[]
                {
                    "Id",
                    "Name",
                    "Description",
                    "Price",
                    "SKU",
                    "StockQuantity",
                    "IsActive",
                    "CategoryId",
                    "CreatedAt",
                    "UpdateAt",
                    "CreatedBy",
                    "UpdatedBy"
                },
                values: new object[,]
                {
                    {
                        new Guid("d1000001-0001-0001-0001-000000000001"),
                        "iPhone 15 Pro",
                        "Apple iPhone 15 Pro 256GB Titanyum. A17 Pro cip, 48MP kamera sistemi, USB-C baglanti.",
                        49999.99m,
                        "APL-IP15P-256",
                        25,
                        true,
                        new Guid("a1111111-1111-1111-1111-111111111111"),
                        new DateTime(2026, 5, 29, 0, 0, 0, DateTimeKind.Utc),
                        null,
                        null,
                        null
                    },
                    {
                        new Guid("d1000002-0002-0002-0002-000000000002"),
                        "Samsung Galaxy S24 Ultra",
                        "Samsung Galaxy S24 Ultra 512GB Titanyum Siyah. S Pen dahil, 200MP kamera.",
                        44999.99m,
                        "SAM-S24U-512",
                        18,
                        true,
                        new Guid("a1111111-1111-1111-1111-111111111111"),
                        new DateTime(2026, 5, 29, 0, 0, 0, DateTimeKind.Utc),
                        null,
                        null,
                        null
                    },
                    {
                        new Guid("d1000003-0003-0003-0003-000000000003"),
                        "MacBook Air M3",
                        "Apple MacBook Air 13 inc M3 cip, 16GB RAM, 512GB SSD. Gece Yarisi rengi.",
                        54999.99m,
                        "APL-MBA-M3-512",
                        12,
                        true,
                        new Guid("a1111111-1111-1111-1111-111111111111"),
                        new DateTime(2026, 5, 29, 0, 0, 0, DateTimeKind.Utc),
                        null,
                        null,
                        null
                    },
                    {
                        new Guid("d1000004-0004-0004-0004-000000000004"),
                        "Sony WH-1000XM5",
                        "Sony WH-1000XM5 kablosuz gurultu engelleme kulaklik. 30 saat pil omru.",
                        8999.99m,
                        "SNY-WH1000XM5",
                        40,
                        true,
                        new Guid("a1111111-1111-1111-1111-111111111111"),
                        new DateTime(2026, 5, 29, 0, 0, 0, DateTimeKind.Utc),
                        null,
                        null,
                        null
                    },
                    {
                        new Guid("d1000005-0005-0005-0005-000000000005"),
                        "iPad Pro 12.9 M4",
                        "Apple iPad Pro 12.9 inc M4 cip, 256GB Wi-Fi. Ultra Retina XDR ekran.",
                        39999.99m,
                        "APL-IPADPRO-M4",
                        15,
                        true,
                        new Guid("a1111111-1111-1111-1111-111111111111"),
                        new DateTime(2026, 5, 29, 0, 0, 0, DateTimeKind.Utc),
                        null,
                        null,
                        null
                    },
                    {
                        new Guid("d2000001-0001-0001-0001-000000000001"),
                        "Nike Air Force 1 '07",
                        "Nike Air Force 1 beyaz unisex spor ayakkabi. Klasik basket stili, deri ust.",
                        3299.99m,
                        "NKE-AF1-WHT-42",
                        50,
                        true,
                        new Guid("b2222222-2222-2222-2222-222222222222"),
                        new DateTime(2026, 5, 29, 0, 0, 0, DateTimeKind.Utc),
                        null,
                        null,
                        null
                    },
                    {
                        new Guid("d2000002-0002-0002-0002-000000000002"),
                        "Levi's 501 Original Jeans",
                        "Levi's 501 Original Fit Jean. Klasik duz kesim, %100 pamuk denim.",
                        1899.99m,
                        "LVS-501-BLU-32",
                        35,
                        true,
                        new Guid("b2222222-2222-2222-2222-222222222222"),
                        new DateTime(2026, 5, 29, 0, 0, 0, DateTimeKind.Utc),
                        null,
                        null,
                        null
                    },
                    {
                        new Guid("d2000003-0003-0003-0003-000000000003"),
                        "Adidas Originals Hoodie",
                        "Adidas Originals erkek kapusonlu sweatshirt. Trefoil logo, pamuklu kumas.",
                        1499.99m,
                        "ADS-HOOD-BLK-L",
                        45,
                        true,
                        new Guid("b2222222-2222-2222-2222-222222222222"),
                        new DateTime(2026, 5, 29, 0, 0, 0, DateTimeKind.Utc),
                        null,
                        null,
                        null
                    },
                    {
                        new Guid("d3000001-0001-0001-0001-000000000001"),
                        "Philips Airfryer XXL",
                        "Philips Airfryer XXL 7.2L dijital. %90 daha az yag ile lezzetli yemekler.",
                        4999.99m,
                        "PHL-AFR-XXL-7L",
                        20,
                        true,
                        new Guid("c3333333-3333-3333-3333-333333333333"),
                        new DateTime(2026, 5, 29, 0, 0, 0, DateTimeKind.Utc),
                        null,
                        null,
                        null
                    },
                    {
                        new Guid("d3000002-0002-0002-0002-000000000002"),
                        "Nespresso Vertuo Pop",
                        "Nespresso Vertuo Pop kahve makinesi. Centrifusion teknolojisi, 5 farkli fincan boyutu.",
                        2799.99m,
                        "NSP-VPP-BLK",
                        30,
                        true,
                        new Guid("c3333333-3333-3333-3333-333333333333"),
                        new DateTime(2026, 5, 29, 0, 0, 0, DateTimeKind.Utc),
                        null,
                        null,
                        null
                    }
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(table: "Products", keyColumn: "Id", keyValue: new Guid("d1000001-0001-0001-0001-000000000001"));
            migrationBuilder.DeleteData(table: "Products", keyColumn: "Id", keyValue: new Guid("d1000002-0002-0002-0002-000000000002"));
            migrationBuilder.DeleteData(table: "Products", keyColumn: "Id", keyValue: new Guid("d1000003-0003-0003-0003-000000000003"));
            migrationBuilder.DeleteData(table: "Products", keyColumn: "Id", keyValue: new Guid("d1000004-0004-0004-0004-000000000004"));
            migrationBuilder.DeleteData(table: "Products", keyColumn: "Id", keyValue: new Guid("d1000005-0005-0005-0005-000000000005"));
            migrationBuilder.DeleteData(table: "Products", keyColumn: "Id", keyValue: new Guid("d2000001-0001-0001-0001-000000000001"));
            migrationBuilder.DeleteData(table: "Products", keyColumn: "Id", keyValue: new Guid("d2000002-0002-0002-0002-000000000002"));
            migrationBuilder.DeleteData(table: "Products", keyColumn: "Id", keyValue: new Guid("d2000003-0003-0003-0003-000000000003"));
            migrationBuilder.DeleteData(table: "Products", keyColumn: "Id", keyValue: new Guid("d3000001-0001-0001-0001-000000000001"));
            migrationBuilder.DeleteData(table: "Products", keyColumn: "Id", keyValue: new Guid("d3000002-0002-0002-0002-000000000002"));
        }
    }
}
