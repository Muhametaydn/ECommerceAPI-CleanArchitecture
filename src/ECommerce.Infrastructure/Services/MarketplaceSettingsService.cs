using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Features.Admin.DTOs;
using ECommerce.Domain.Entities;
using ECommerce.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Services
{
    public sealed class MarketplaceSettingsService : IMarketplaceSettingsService
    {
        private readonly ApplicationDbContext _dbContext;

        public MarketplaceSettingsService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<MarketplaceSettingsDTO> GetAsync(CancellationToken cancellationToken = default)
        {
            var settings = await GetOrCreateAsync(cancellationToken);
            return new MarketplaceSettingsDTO
            {
                AutoApproveSellerProducts = settings.AutoApproveSellerProducts
            };
        }

        public async Task<MarketplaceSettingsDTO> SetAutoApprovalAsync(
            bool enabled,
            CancellationToken cancellationToken = default)
        {
            var settings = await GetOrCreateAsync(cancellationToken);
            settings.SetAutoApproval(enabled);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return new MarketplaceSettingsDTO
            {
                AutoApproveSellerProducts = settings.AutoApproveSellerProducts
            };
        }

        private async Task<MarketplaceSettings> GetOrCreateAsync(CancellationToken cancellationToken)
        {
            var settings = await _dbContext.MarketplaceSettings
                .SingleOrDefaultAsync(item => item.Id == MarketplaceSettings.DefaultId, cancellationToken);

            if (settings is not null)
                return settings;

            settings = new MarketplaceSettings { Id = MarketplaceSettings.DefaultId };
            _dbContext.MarketplaceSettings.Add(settings);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return settings;
        }
    }
}
