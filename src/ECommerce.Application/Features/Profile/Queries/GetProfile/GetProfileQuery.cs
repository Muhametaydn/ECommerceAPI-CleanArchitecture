using ECommerce.Application.Features.Profiles.DTOs;
using MediatR;

namespace ECommerce.Application.Features.Profiles.Queries.GetProfile;

public sealed record GetProfileQuery(Guid UserId) : IRequest<UserProfileDTO>;
