using ECommerce.Application.Common.Interfaces;
using MediatR;

namespace ECommerce.Application.Features.Auth.Commands.ResetPassword;

public sealed class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand>
{
    private readonly IAccountSecurityService _securityService;

    public ResetPasswordCommandHandler(IAccountSecurityService securityService) => _securityService = securityService;

    public Task Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
        => _securityService.ResetPasswordAsync(request.Email, request.Token, request.NewPassword, cancellationToken);
}
