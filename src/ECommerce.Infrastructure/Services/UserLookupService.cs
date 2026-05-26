using ECommerce.Application.Common.Interfaces;
using ECommerce.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace ECommerce.Infrastructure.Services
{
    public sealed class UserLookupService : IUserLookupService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<UserLookupService> _logger;

        public UserLookupService(UserManager<ApplicationUser> userManager, ILogger<UserLookupService> logger)
        {
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<(string Email, string FullName)?> GetUserInfoAsync(Guid userId, CancellationToken ct = default)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user is null)
            {
                _logger.LogWarning("UserLookup: Kullanıcı bulunamadı. Id: {UserId}", userId);
                return null;
            }

            var email = user.Email ?? string.Empty;
            var fullName = $"{user.FirstName} {user.LastName}".Trim();

            return (email, fullName);
        }
    }
}
