using ECommerce.Application.Features.Admin.DTOs;

namespace ECommerce.Application.Common.Interfaces
{
    public interface IMarketplaceSettingsService
    {
        Task<MarketplaceSettingsDTO> GetAsync(CancellationToken cancellationToken = default);
        Task<MarketplaceSettingsDTO> SetAutoApprovalAsync(bool enabled, CancellationToken cancellationToken = default);
    }
}
