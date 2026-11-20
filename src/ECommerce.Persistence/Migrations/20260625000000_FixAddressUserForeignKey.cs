using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixAddressUserForeignKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE "Address"
                DROP CONSTRAINT IF EXISTS "FK_Address_Users_UserId";
                """);

            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1
                        FROM pg_constraint
                        WHERE conname = 'FK_Address_AspNetUsers_UserId'
                    ) THEN
                        ALTER TABLE "Address"
                        ADD CONSTRAINT "FK_Address_AspNetUsers_UserId"
                        FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id")
                        ON DELETE CASCADE;
                    END IF;
                END $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE "Address"
                DROP CONSTRAINT IF EXISTS "FK_Address_AspNetUsers_UserId";
                """);

            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1
                        FROM pg_constraint
                        WHERE conname = 'FK_Address_Users_UserId'
                    ) THEN
                        ALTER TABLE "Address"
                        ADD CONSTRAINT "FK_Address_Users_UserId"
                        FOREIGN KEY ("UserId") REFERENCES "Users" ("Id")
                        ON DELETE CASCADE;
                    END IF;
                END $$;
                """);
        }
    }
}
