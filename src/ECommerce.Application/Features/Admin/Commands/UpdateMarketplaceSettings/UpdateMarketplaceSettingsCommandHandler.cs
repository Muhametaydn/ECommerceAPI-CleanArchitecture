using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Features.Admin.DTOs;
using MediatR;

namespace ECommerce.Application.Features.Admin.Commands.UpdateMarketplaceSettings
{
    public class UpdateMarketplaceSettingsCommandHandler : IRequestHandler<UpdateMarketplaceSettingsCommand, MarketplaceSettingsDTO>
    {
        private readonly IMarketplaceSettingsService _settingsService;

        public UpdateMarketplaceSettingsCommandHandler(IMarketplaceSettingsService settingsService)
        {
            _settingsService = settingsService;
        }

        public Task<MarketplaceSettingsDTO> Handle(UpdateMarketplaceSettingsCommand request, CancellationToken cancellationToken)
            => _settingsService.SetAutoApprovalAsync(request.AutoApproveSellerProducts, cancellationToken);
    }
}
