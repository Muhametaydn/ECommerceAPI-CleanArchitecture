namespace ECommerce.Application.Features.Profiles.DTOs;

public sealed class UserProfileDTO
{
    public Guid Id { get; init; }
    public string Email { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string? ProfileImageUrl { get; init; }
    public IReadOnlyList<string> Roles { get; init; } = [];
}
