using ECommerce.Application.Common.Interfaces;
using MediatR;

namespace ECommerce.Application.Features.Profiles.Commands.ChangePassword;

public sealed class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand>
{
    private readonly IAccountSecurityService _securityService;

    public ChangePasswordCommandHandler(IAccountSecurityService securityService) => _securityService = securityService;

    public Task Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
        => _securityService.ChangePasswordAsync(request.UserId, request.CurrentPassword, request.NewPassword, cancellationToken);
}
