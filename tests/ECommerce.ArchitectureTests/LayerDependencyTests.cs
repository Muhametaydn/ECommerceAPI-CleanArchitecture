using System.Reflection;
using ECommerce.Domain.Entities;
using ECommerce.Application.Common.Behaviors;
using ECommerce.Persistence.Context;

namespace ECommerce.ArchitectureTests;

public class LayerDependencyTests
{
    private static readonly string[] ApplicationForbiddenReferences =
    [
        "ECommerce.Infrastructure",
        "ECommerce.Persistence",
        "ECommerce.API"
    ];

    private static readonly string[] DomainForbiddenReferences =
    [
        "ECommerce.Application",
        "ECommerce.Infrastructure",
        "ECommerce.Persistence",
        "ECommerce.API"
    ];

    [Fact]
    public void Domain_Should_Not_Depend_On_Other_Project_Layers()
    {
        var references = GetReferencedAssemblyNames(typeof(Product).Assembly);

        Assert.DoesNotContain(references, DomainForbiddenReferences.Contains);
    }

    [Fact]
    public void Application_Should_Not_Depend_On_Outer_Layers()
    {
        var references = GetReferencedAssemblyNames(typeof(ValidationBehavior<,>).Assembly);

        Assert.DoesNotContain(references, ApplicationForbiddenReferences.Contains);
    }

    [Fact]
    public void Persistence_Should_Not_Depend_On_Infrastructure_Or_Api()
    {
        var references = GetReferencedAssemblyNames(typeof(ApplicationDbContext).Assembly);

        Assert.DoesNotContain("ECommerce.Infrastructure", references);
        Assert.DoesNotContain("ECommerce.API", references);
    }

    [Fact]
    public void Infrastructure_Should_Not_Depend_On_Api()
    {
        var references = GetReferencedAssemblyNames(typeof(ECommerce.Infrastructure.ServiceRegistration).Assembly);

        Assert.DoesNotContain("ECommerce.API", references);
    }

    private static HashSet<string> GetReferencedAssemblyNames(Assembly assembly)
    {
        return assembly
            .GetReferencedAssemblies()
            .Select(x => x.Name!)
            .ToHashSet(StringComparer.Ordinal);
    }
}
