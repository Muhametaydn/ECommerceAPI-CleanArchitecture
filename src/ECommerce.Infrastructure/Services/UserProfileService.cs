using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Features.Profiles.DTOs;
using ECommerce.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace ECommerce.Infrastructure.Services;

public sealed class UserProfileService : IUserProfileService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IProfileImageStorage _imageStorage;

    public UserProfileService(UserManager<ApplicationUser> userManager, IProfileImageStorage imageStorage)
    {
        _userManager = userManager;
        _imageStorage = imageStorage;
    }

    public async Task<UserProfileDTO> GetAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString()) ?? throw new KeyNotFoundException("Kullanıcı bulunamadı.");
        return await MapAsync(user);
    }

    public async Task<UserProfileDTO> UpdateAsync(Guid userId, string firstName, string lastName, string? profileImageUrl, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString()) ?? throw new KeyNotFoundException("Kullanıcı bulunamadı.");
        var previousImageUrl = user.ProfileImageUrl;

        user.UpdateProfile(firstName, lastName, profileImageUrl);
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join(" ", result.Errors.Select(error => error.Description)));

        if (!string.Equals(previousImageUrl, user.ProfileImageUrl, StringComparison.OrdinalIgnoreCase))
            await _imageStorage.DeleteIfUnreferencedAsync(previousImageUrl, cancellationToken);

        return await MapAsync(user);
    }

    private async Task<UserProfileDTO> MapAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        return new UserProfileDTO
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            FirstName = user.FirstName,
            LastName = user.LastName,
            ProfileImageUrl = user.ProfileImageUrl,
            Roles = roles.ToArray()
        };
    }
}
