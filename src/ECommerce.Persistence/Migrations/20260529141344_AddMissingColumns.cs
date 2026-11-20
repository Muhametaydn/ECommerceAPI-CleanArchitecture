using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMissingColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE \"Orders\" ADD COLUMN IF NOT EXISTS \"CouponCode\" character varying(50) NULL;");
            migrationBuilder.Sql("ALTER TABLE \"Orders\" ADD COLUMN IF NOT EXISTS \"DiscountAmount\" numeric(18,2) NOT NULL DEFAULT 0;");
            migrationBuilder.Sql("ALTER TABLE \"Coupons\" ADD COLUMN IF NOT EXISTS \"IsActive\" boolean NOT NULL DEFAULT TRUE;");

            migrationBuilder.Sql("ALTER TABLE \"Categories\" ALTER COLUMN \"Description\" TYPE character varying(500);");
            migrationBuilder.Sql("ALTER TABLE \"Categories\" ADD COLUMN IF NOT EXISTS \"Depth\" integer NOT NULL DEFAULT 1;");
            migrationBuilder.Sql("ALTER TABLE \"Categories\" ADD COLUMN IF NOT EXISTS \"IsActive\" boolean NOT NULL DEFAULT TRUE;");
            migrationBuilder.Sql("ALTER TABLE \"Categories\" ADD COLUMN IF NOT EXISTS \"SortOrder\" integer NOT NULL DEFAULT 0;");

            migrationBuilder.Sql("""
                UPDATE "Categories"
                SET "Depth" = 1,
                    "IsActive" = TRUE,
                    "SortOrder" = CASE "Id"
                        WHEN 'a1111111-1111-1111-1111-111111111111' THEN 0
                        WHEN 'b2222222-2222-2222-2222-222222222222' THEN 1
                        WHEN 'c3333333-3333-3333-3333-333333333333' THEN 2
                        ELSE "SortOrder"
                    END
                WHERE "Id" IN (
                    'a1111111-1111-1111-1111-111111111111',
                    'b2222222-2222-2222-2222-222222222222',
                    'c3333333-3333-3333-3333-333333333333'
                );
                """);

            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS \"IX_Categories_ParentCategoryId_SortOrder\" ON \"Categories\" (\"ParentCategoryId\", \"SortOrder\");");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_Categories_ParentCategoryId_SortOrder\";");
            migrationBuilder.Sql("ALTER TABLE \"Orders\" DROP COLUMN IF EXISTS \"CouponCode\";");
            migrationBuilder.Sql("ALTER TABLE \"Orders\" DROP COLUMN IF EXISTS \"DiscountAmount\";");
            migrationBuilder.Sql("ALTER TABLE \"Coupons\" DROP COLUMN IF EXISTS \"IsActive\";");
            migrationBuilder.Sql("ALTER TABLE \"Categories\" DROP COLUMN IF EXISTS \"Depth\";");
            migrationBuilder.Sql("ALTER TABLE \"Categories\" DROP COLUMN IF EXISTS \"IsActive\";");
            migrationBuilder.Sql("ALTER TABLE \"Categories\" DROP COLUMN IF EXISTS \"SortOrder\";");
            migrationBuilder.Sql("ALTER TABLE \"Categories\" ALTER COLUMN \"Description\" TYPE text;");
        }
    }
}