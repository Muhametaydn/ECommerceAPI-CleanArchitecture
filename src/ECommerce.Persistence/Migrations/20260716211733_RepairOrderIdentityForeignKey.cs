using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.Persistence.Migrations
{
    public partial class RepairOrderIdentityForeignKey : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE "Orders"
                DROP CONSTRAINT IF EXISTS "FK_Orders_Users_UserId";

                ALTER TABLE "Orders"
                DROP CONSTRAINT IF EXISTS "FK_Orders_AspNetUsers_UserId";

                ALTER TABLE "Orders"
                ADD CONSTRAINT "FK_Orders_AspNetUsers_UserId"
                FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id")
                ON DELETE RESTRICT;

                ALTER TABLE "Address"
                DROP CONSTRAINT IF EXISTS "FK_Address_Users_UserId";

                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_constraint
                        WHERE conname = 'FK_Address_AspNetUsers_UserId'
                          AND conrelid = '"Address"'::regclass
                    ) THEN
                        ALTER TABLE "Address"
                        ADD CONSTRAINT "FK_Address_AspNetUsers_UserId"
                        FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id")
                        ON DELETE CASCADE;
                    END IF;
                END $$;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE "Orders"
                DROP CONSTRAINT IF EXISTS "FK_Orders_AspNetUsers_UserId";

                ALTER TABLE "Orders"
                ADD CONSTRAINT "FK_Orders_Users_UserId"
                FOREIGN KEY ("UserId") REFERENCES "Users" ("Id")
                ON DELETE RESTRICT
                NOT VALID;
                """);
        }
    }
}
