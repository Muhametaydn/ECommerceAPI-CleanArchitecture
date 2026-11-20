using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Features.Profiles.DTOs;
using MediatR;

namespace ECommerce.Application.Features.Profiles.Queries.GetProfile;

public sealed class GetProfileQueryHandler : IRequestHandler<GetProfileQuery, UserProfileDTO>
{
    private readonly IUserProfileService _profileService;

    public GetProfileQueryHandler(IUserProfileService profileService) => _profileService = profileService;

    public Task<UserProfileDTO> Handle(GetProfileQuery request, CancellationToken cancellationToken)
        => _profileService.GetAsync(request.UserId, cancellationToken);
}
