using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProductImageUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "Products",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE "Products"
                SET "ImageUrl" = CASE "Id"
                    WHEN 'd1000001-0001-0001-0001-000000000001' THEN 'https://images.unsplash.com/photo-1592750475338-74b7b21085ab?auto=format&fit=crop&w=1200&q=80'
                    WHEN 'd1000002-0002-0002-0002-000000000002' THEN 'https://images.unsplash.com/photo-1511707171634-5f897ff02aa9?auto=format&fit=crop&w=1200&q=80'
                    WHEN 'd1000003-0003-0003-0003-000000000003' THEN 'https://images.unsplash.com/photo-1517336714731-489689fd1ca8?auto=format&fit=crop&w=1200&q=80'
                    WHEN 'd1000004-0004-0004-0004-000000000004' THEN 'https://images.unsplash.com/photo-1505740420928-5e560c06d30e?auto=format&fit=crop&w=1200&q=80'
                    WHEN 'd1000005-0005-0005-0005-000000000005' THEN 'https://images.unsplash.com/photo-1544244015-0df4b3ffc6b0?auto=format&fit=crop&w=1200&q=80'
                    WHEN 'd2000001-0001-0001-0001-000000000001' THEN 'https://images.unsplash.com/photo-1542291026-7eec264c27ff?auto=format&fit=crop&w=1200&q=80'
                    WHEN 'd2000002-0002-0002-0002-000000000002' THEN 'https://images.unsplash.com/photo-1542272604-787c3835535d?auto=format&fit=crop&w=1200&q=80'
                    WHEN 'd2000003-0003-0003-0003-000000000003' THEN 'https://images.unsplash.com/photo-1556821840-3a63f95609a7?auto=format&fit=crop&w=1200&q=80'
                    WHEN 'd3000001-0001-0001-0001-000000000001' THEN 'https://images.unsplash.com/photo-1585515320310-259814833e62?auto=format&fit=crop&w=1200&q=80'
                    WHEN 'd3000002-0002-0002-0002-000000000002' THEN 'https://images.unsplash.com/photo-1517668808822-9ebb02f2a0e6?auto=format&fit=crop&w=1200&q=80'
                    ELSE "ImageUrl"
                END
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "Products");
        }
    }
}
