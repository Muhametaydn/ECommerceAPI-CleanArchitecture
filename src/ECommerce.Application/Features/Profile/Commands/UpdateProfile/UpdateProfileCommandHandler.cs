using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Features.Profiles.DTOs;
using MediatR;

namespace ECommerce.Application.Features.Profiles.Commands.UpdateProfile;

public sealed class UpdateProfileCommandHandler : IRequestHandler<UpdateProfileCommand, UserProfileDTO>
{
    private readonly IUserProfileService _profileService;

    public UpdateProfileCommandHandler(IUserProfileService profileService) => _profileService = profileService;

    public Task<UserProfileDTO> Handle(UpdateProfileCommand request, CancellationToken cancellationToken)
        => _profileService.UpdateAsync(request.UserId, request.FirstName, request.LastName, request.ProfileImageUrl, cancellationToken);
}
