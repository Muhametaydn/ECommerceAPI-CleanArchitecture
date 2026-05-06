using ECommerce.Application.Features.Categories.DTOs;
using MediatR;

namespace ECommerce.Application.Features.Categories.Queries.GetBreadcrumb;

/// <summary>
/// Belirtilen kategorinin breadcrumb yolunu getirir.
/// Ornek: Elektronik > Telefonlar > Akilli Telefonlar
/// </summary>
public record GetBreadcrumbQuery(Guid CategoryId) : IRequest<BreadcrumbDTO>;
