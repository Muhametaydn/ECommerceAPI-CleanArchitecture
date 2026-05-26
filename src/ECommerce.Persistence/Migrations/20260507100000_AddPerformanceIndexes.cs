using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── Products ──────────────────────────────────────────────────────
            // Kategori bazlı filtreleme
            migrationBuilder.CreateIndex(
                name: "IX_Products_CategoryId",
                table: "Products",
                column: "CategoryId");

            // Aktif ürün + fiyat sıralaması
            migrationBuilder.CreateIndex(
                name: "IX_Products_IsActive_Price",
                table: "Products",
                columns: new[] { "IsActive", "Price" });

            // Aktif ürün + tarih sıralaması (varsayılan sıralama)
            migrationBuilder.CreateIndex(
                name: "IX_Products_IsActive_CreatedAt",
                table: "Products",
                columns: new[] { "IsActive", "CreatedAt" });

            // Aktif ürün + kategori + fiyat aralığı
            migrationBuilder.CreateIndex(
                name: "IX_Products_CategoryId_IsActive_Price",
                table: "Products",
                columns: new[] { "CategoryId", "IsActive", "Price" });

            // ── Orders ────────────────────────────────────────────────────────
            // Kullanıcı siparişleri
            migrationBuilder.CreateIndex(
                name: "IX_Orders_UserId",
                table: "Orders",
                column: "UserId");

            // Kullanıcı + durum filtresi
            migrationBuilder.CreateIndex(
                name: "IX_Orders_UserId_Status",
                table: "Orders",
                columns: new[] { "UserId", "Status" });

            // Sipariş durum filtresi (admin sorguları)
            migrationBuilder.CreateIndex(
                name: "IX_Orders_Status",
                table: "Orders",
                column: "Status");

            // Tarih sıralaması
            migrationBuilder.CreateIndex(
                name: "IX_Orders_CreatedAt",
                table: "Orders",
                column: "CreatedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "IX_Products_CategoryId", table: "Products");
            migrationBuilder.DropIndex(name: "IX_Products_IsActive_Price", table: "Products");
            migrationBuilder.DropIndex(name: "IX_Products_IsActive_CreatedAt", table: "Products");
            migrationBuilder.DropIndex(name: "IX_Products_CategoryId_IsActive_Price", table: "Products");
            migrationBuilder.DropIndex(name: "IX_Orders_UserId", table: "Orders");
            migrationBuilder.DropIndex(name: "IX_Orders_UserId_Status", table: "Orders");
            migrationBuilder.DropIndex(name: "IX_Orders_Status", table: "Orders");
            migrationBuilder.DropIndex(name: "IX_Orders_CreatedAt", table: "Orders");
        }
    }
}
