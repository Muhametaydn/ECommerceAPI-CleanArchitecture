using ECommerce.Application.Features.Profiles.DTOs;

namespace ECommerce.Application.Common.Interfaces;

public interface IUserProfileService
{
    Task<UserProfileDTO> GetAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<UserProfileDTO> UpdateAsync(Guid userId, string firstName, string lastName, string? profileImageUrl, CancellationToken cancellationToken = default);
}
