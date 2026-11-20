using ECommerce.Application.Features.Profiles.DTOs;
using MediatR;

namespace ECommerce.Application.Features.Profiles.Commands.UpdateProfile;

public sealed record UpdateProfileCommand(Guid UserId, string FirstName, string LastName, string? ProfileImageUrl) : IRequest<UserProfileDTO>;
