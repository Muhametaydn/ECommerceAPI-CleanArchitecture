using System.Reflection;
using ECommerce.API.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.IntegrationTests;

public class ApiSurfaceTests
{
    [Fact]
    public void Api_Assembly_Should_Expose_Expected_Controllers()
    {
        var controllerNames = typeof(ProductsController).Assembly
            .GetTypes()
            .Where(type => type.IsAssignableTo(typeof(ControllerBase)) && !type.IsAbstract)
            .Select(type => type.Name)
            .ToHashSet(StringComparer.Ordinal);

        string[] expectedControllers =
        [
            nameof(AuthController),
            nameof(ProductsController),
            nameof(OrdersController),
            nameof(CartController),
            nameof(CategoriesController),
            nameof(CouponsController),
            nameof(AddressesController),
            nameof(ReviewsController),
            nameof(PaymentsController)
        ];

        Assert.All(expectedControllers, name => Assert.Contains(name, controllerNames));
    }

    [Fact]
    public void Controllers_Should_Use_Api_V1_Routes()
    {
        var controllers = typeof(ProductsController).Assembly
            .GetTypes()
            .Where(type => type.IsAssignableTo(typeof(ControllerBase)) && !type.IsAbstract);

        foreach (var controller in controllers)
        {
            var route = controller.GetCustomAttribute<RouteAttribute>();

            Assert.NotNull(route);
            Assert.Contains("api/v1", route!.Template, StringComparison.OrdinalIgnoreCase);
        }
    }
}
