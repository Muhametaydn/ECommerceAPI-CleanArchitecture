

using ECommerce.Domain.Constants;
using ECommerce.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.Persistence.Seed;

public static class IdentitySeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider, bool seedDemoUsers = false)
    {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        // Rolleri oluştur
        foreach (var role in AppRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole<Guid>(role));
        }

        // Bilinen parolalara sahip demo hesapları yalnızca açıkça etkinleştirilen
        // yerel/test ortamlarında oluşturulur. Roller her ortamda hazırlanır.
        if (!seedDemoUsers)
            return;

        // Admin kullanıcı
        var adminEmail = "admin@outlook.com";
        if (await userManager.FindByEmailAsync(adminEmail) is null)
        {
            var admin = ApplicationUser.Create("Admin", "User", adminEmail, adminEmail);
            admin.EmailConfirmed = true; // Seed'de direkt onaylı

            var result = await userManager.CreateAsync(admin, "Admin123!");
            if (result.Succeeded)
                await userManager.AddToRoleAsync(admin, AppRoles.Admin);
        }

        // Geliştirme ortamında satıcı panelini deneyebilmek için örnek satıcı
        var sellerEmail = "seller@outlook.com";
        if (await userManager.FindByEmailAsync(sellerEmail) is null)
        {
            var seller = ApplicationUser.Create("Örnek", "Satıcı", sellerEmail, sellerEmail);
            seller.EmailConfirmed = true;

            var result = await userManager.CreateAsync(seller, "Seller123!");
            if (result.Succeeded)
                await userManager.AddToRoleAsync(seller, AppRoles.Seller);
        }
    }
}
