using System.Linq.Expressions;
using ECommerce.Domain.Common;

namespace ECommerce.Domain.Specifications;

/// <summary>
/// Specification Pattern: Filtreleme, siralama ve include islemlerini
/// tek bir nesne icinde kapsulleyen interface.
/// Repository'ye "ne istedigimizi" soyleriz, "nasil yapacagini" degil.
/// </summary>
public interface ISpecification<T> where T : BaseEntity
{
    // WHERE kosulu — ornegin: p => p.IsActive && p.Price > 100
    Expression<Func<T, bool>>? Criteria { get; }

    // Include'lar — ornegin: p => p.Category, p => p.Reviews
    List<Expression<Func<T, object>>> Includes { get; }

    // String-based include'lar — ornegin: "Category.SubCategories" (nested include)
    List<string> IncludeStrings { get; }

    // Siralama
    Expression<Func<T, object>>? OrderBy { get; }
    Expression<Func<T, object>>? OrderByDescending { get; }

    // Sayfalama
    int? Skip { get; }
    int? Take { get; }
    bool IsPagingEnabled { get; }
}
