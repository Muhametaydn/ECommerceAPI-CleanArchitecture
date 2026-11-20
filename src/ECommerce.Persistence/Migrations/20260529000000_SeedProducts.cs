using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ECommerce.Persistence.Context;

#nullable disable

namespace ECommerce.Persistence.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260529000000_SeedProducts")]
    public partial class SeedProducts : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO "Products"
                    ("Id", "Name", "Description", "Price", "SKU", "StockQuantity", "IsActive", "CategoryId", "CreatedAt", "UpdateAt", "CreatedBy", "UpdatedBy")
                VALUES
                    ('d1000001-0001-0001-0001-000000000001', 'iPhone 15 Pro', 'Apple iPhone 15 Pro 256GB Titanyum. A17 Pro cip, 48MP kamera sistemi, USB-C baglanti.', 49999.99, 'APL-IP15P-256', 25, TRUE, 'a1111111-1111-1111-1111-111111111111', '2026-05-29T00:00:00Z', NULL, NULL, NULL),
                    ('d1000002-0002-0002-0002-000000000002', 'Samsung Galaxy S24 Ultra', 'Samsung Galaxy S24 Ultra 512GB Titanyum Siyah. S Pen dahil, 200MP kamera.', 44999.99, 'SAM-S24U-512', 18, TRUE, 'a1111111-1111-1111-1111-111111111111', '2026-05-29T00:00:00Z', NULL, NULL, NULL),
                    ('d1000003-0003-0003-0003-000000000003', 'MacBook Air M3', 'Apple MacBook Air 13 inc M3 cip, 16GB RAM, 512GB SSD. Gece Yarisi rengi.', 54999.99, 'APL-MBA-M3-512', 12, TRUE, 'a1111111-1111-1111-1111-111111111111', '2026-05-29T00:00:00Z', NULL, NULL, NULL),
                    ('d1000004-0004-0004-0004-000000000004', 'Sony WH-1000XM5', 'Sony WH-1000XM5 kablosuz gurultu engelleme kulaklik. 30 saat pil omru.', 8999.99, 'SNY-WH1000XM5', 40, TRUE, 'a1111111-1111-1111-1111-111111111111', '2026-05-29T00:00:00Z', NULL, NULL, NULL),
                    ('d1000005-0005-0005-0005-000000000005', 'iPad Pro 12.9 M4', 'Apple iPad Pro 12.9 inc M4 cip, 256GB Wi-Fi. Ultra Retina XDR ekran.', 39999.99, 'APL-IPADPRO-M4', 15, TRUE, 'a1111111-1111-1111-1111-111111111111', '2026-05-29T00:00:00Z', NULL, NULL, NULL),
                    ('d2000001-0001-0001-0001-000000000001', 'Nike Air Force 1 ''07', 'Nike Air Force 1 beyaz unisex spor ayakkabi. Klasik basket stili, deri ust.', 3299.99, 'NKE-AF1-WHT-42', 50, TRUE, 'b2222222-2222-2222-2222-222222222222', '2026-05-29T00:00:00Z', NULL, NULL, NULL),
                    ('d2000002-0002-0002-0002-000000000002', 'Levi''s 501 Original Jeans', 'Levi''s 501 Original Fit Jean. Klasik duz kesim, %100 pamuk denim.', 1899.99, 'LVS-501-BLU-32', 35, TRUE, 'b2222222-2222-2222-2222-222222222222', '2026-05-29T00:00:00Z', NULL, NULL, NULL),
                    ('d2000003-0003-0003-0003-000000000003', 'Adidas Originals Hoodie', 'Adidas Originals erkek kapusonlu sweatshirt. Trefoil logo, pamuklu kumas.', 1499.99, 'ADS-HOOD-BLK-L', 45, TRUE, 'b2222222-2222-2222-2222-222222222222', '2026-05-29T00:00:00Z', NULL, NULL, NULL),
                    ('d3000001-0001-0001-0001-000000000001', 'Philips Airfryer XXL', 'Philips Airfryer XXL 7.2L dijital. %90 daha az yag ile lezzetli yemekler.', 4999.99, 'PHL-AFR-XXL-7L', 20, TRUE, 'c3333333-3333-3333-3333-333333333333', '2026-05-29T00:00:00Z', NULL, NULL, NULL),
                    ('d3000002-0002-0002-0002-000000000002', 'Nespresso Vertuo Pop', 'Nespresso Vertuo Pop kahve makinesi. Centrifusion teknolojisi, 5 farkli fincan boyutu.', 2799.99, 'NSP-VPP-BLK', 30, TRUE, 'c3333333-3333-3333-3333-333333333333', '2026-05-29T00:00:00Z', NULL, NULL, NULL)
                ON CONFLICT ("Id") DO NOTHING;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM "Products"
                WHERE "Id" IN (
                    'd1000001-0001-0001-0001-000000000001',
                    'd1000002-0002-0002-0002-000000000002',
                    'd1000003-0003-0003-0003-000000000003',
                    'd1000004-0004-0004-0004-000000000004',
                    'd1000005-0005-0005-0005-000000000005',
                    'd2000001-0001-0001-0001-000000000001',
                    'd2000002-0002-0002-0002-000000000002',
                    'd2000003-0003-0003-0003-000000000003',
                    'd3000001-0001-0001-0001-000000000001',
                    'd3000002-0002-0002-0002-000000000002'
                );
                """);
        }
    }
}