using ECommerce.Application.Features.Admin.DTOs;
using MediatR;

namespace ECommerce.Application.Features.Admin.Commands.UpdateMarketplaceSettings
{
    public record UpdateMarketplaceSettingsCommand(bool AutoApproveSellerProducts) : IRequest<MarketplaceSettingsDTO>;
}
