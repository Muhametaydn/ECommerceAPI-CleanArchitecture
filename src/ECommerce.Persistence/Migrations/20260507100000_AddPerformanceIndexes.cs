using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ECommerce.Persistence.Context;

#nullable disable

namespace ECommerce.Persistence.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260507100000_AddPerformanceIndexes")]
    public partial class AddPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS \"IX_Products_CategoryId\" ON \"Products\" (\"CategoryId\");");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS \"IX_Products_IsActive_Price\" ON \"Products\" (\"IsActive\", \"Price\");");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS \"IX_Products_IsActive_CreatedAt\" ON \"Products\" (\"IsActive\", \"CreatedAt\");");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS \"IX_Products_CategoryId_IsActive_Price\" ON \"Products\" (\"CategoryId\", \"IsActive\", \"Price\");");

            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS \"IX_Orders_UserId\" ON \"Orders\" (\"UserId\");");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS \"IX_Orders_UserId_Status\" ON \"Orders\" (\"UserId\", \"Status\");");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS \"IX_Orders_Status\" ON \"Orders\" (\"Status\");");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS \"IX_Orders_CreatedAt\" ON \"Orders\" (\"CreatedAt\");");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_Orders_CreatedAt\";");
            migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_Orders_Status\";");
            migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_Orders_UserId_Status\";");
            migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_Orders_UserId\";");

            migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_Products_CategoryId_IsActive_Price\";");
            migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_Products_IsActive_CreatedAt\";");
            migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_Products_IsActive_Price\";");
            migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_Products_CategoryId\";");
        }
    }
}