using ECommerce.Application.Features.Admin.DTOs;
using MediatR;

namespace ECommerce.Application.Features.Admin.Queries.GetMarketplaceSettings
{
    public record GetMarketplaceSettingsQuery : IRequest<MarketplaceSettingsDTO>;
}
