using ECommerce.Persistence.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ECommerce.Persistence.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260929183000_ConfirmExistingIdentityUsers")]
public sealed class ConfirmExistingIdentityUsers : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("UPDATE \"AspNetUsers\" SET \"EmailConfirmed\" = TRUE WHERE \"EmailConfirmed\" = FALSE;");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }
}
