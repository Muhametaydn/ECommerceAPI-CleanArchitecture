using ECommerce.Persistence.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ECommerce.Persistence.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261007190000_AddCartCheckoutKey")]
public sealed class AddCartCheckoutKey : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(name: "CartCheckoutKey", table: "Orders", type: "character varying(64)", maxLength: 64, nullable: true);
        migrationBuilder.CreateIndex(name: "IX_Orders_CartCheckoutKey", table: "Orders", column: "CartCheckoutKey", unique: true);
    }
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(name: "IX_Orders_CartCheckoutKey", table: "Orders");
        migrationBuilder.DropColumn(name: "CartCheckoutKey", table: "Orders");
    }
}