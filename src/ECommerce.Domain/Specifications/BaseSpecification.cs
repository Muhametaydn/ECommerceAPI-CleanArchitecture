using System.Linq.Expressions;
using ECommerce.Domain.Common;

namespace ECommerce.Domain.Specifications;

/// <summary>
/// Specification'larin base sinifi.
/// Alt siniflar constructor'da AddCriteria, AddInclude, ApplyOrderBy gibi
/// method'lari cagirarak sorgu kosullarini tanimlar.
/// </summary>
public abstract class BaseSpecification<T> : ISpecification<T> where T : BaseEntity
{
    public Expression<Func<T, bool>>? Criteria { get; private set; }
    public List<Expression<Func<T, object>>> Includes { get; } = new();
    public List<string> IncludeStrings { get; } = new();
    public Expression<Func<T, object>>? OrderBy { get; private set; }
    public Expression<Func<T, object>>? OrderByDescending { get; private set; }
    public int? Skip { get; private set; }
    public int? Take { get; private set; }
    public bool IsPagingEnabled { get; private set; }

    // Parametresiz constructor — tum kayitlari getirir
    protected BaseSpecification() { }

    // Kriterli constructor — WHERE kosulu ile baslar
    protected BaseSpecification(Expression<Func<T, bool>> criteria)
    {
        Criteria = criteria;
    }

    // Alt siniflar bu method'lari kullanarak specification'i sekillendirir
    protected void AddCriteria(Expression<Func<T, bool>> criteria) => Criteria = criteria;
    protected void AddInclude(Expression<Func<T, object>> includeExpression) => Includes.Add(includeExpression);
    protected void AddInclude(string includeString) => IncludeStrings.Add(includeString);
    protected void ApplyOrderBy(Expression<Func<T, object>> orderBy) => OrderBy = orderBy;
    protected void ApplyOrderByDescending(Expression<Func<T, object>> orderByDesc) => OrderByDescending = orderByDesc;

    protected void ApplyPaging(int skip, int take)
    {
        Skip = skip;
        Take = take;
        IsPagingEnabled = true;
    }
}
