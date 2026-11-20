using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Features.Admin.DTOs;
using MediatR;

namespace ECommerce.Application.Features.Admin.Queries.GetMarketplaceSettings
{
    public class GetMarketplaceSettingsQueryHandler : IRequestHandler<GetMarketplaceSettingsQuery, MarketplaceSettingsDTO>
    {
        private readonly IMarketplaceSettingsService _settingsService;

        public GetMarketplaceSettingsQueryHandler(IMarketplaceSettingsService settingsService)
        {
            _settingsService = settingsService;
        }

        public Task<MarketplaceSettingsDTO> Handle(GetMarketplaceSettingsQuery request, CancellationToken cancellationToken)
            => _settingsService.GetAsync(cancellationToken);
    }
}
