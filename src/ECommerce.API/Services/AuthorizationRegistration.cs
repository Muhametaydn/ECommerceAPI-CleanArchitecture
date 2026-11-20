namespace ECommerce.API.Services;

public static class AuthorizationRegistration
{
    public static IServiceCollection AddMarketplaceAuthorization(this IServiceCollection services)
    {
        services.AddAuthorizationBuilder()
            .AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"))
            .AddPolicy("SellerOrAdmin", policy => policy.RequireRole("Admin", "Seller"))
            .AddPolicy("CustomerOnly", policy => policy.RequireRole("Customer"))
            .AddPolicy("Authenticated", policy => policy.RequireAuthenticatedUser());
        return services;
    }
}