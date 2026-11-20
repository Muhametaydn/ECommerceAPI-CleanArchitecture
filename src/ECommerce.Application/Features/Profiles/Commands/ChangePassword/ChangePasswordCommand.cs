using MediatR;

namespace ECommerce.Application.Features.Profiles.Commands.ChangePassword;

public sealed record ChangePasswordCommand(Guid UserId, string CurrentPassword, string NewPassword) : IRequest;
