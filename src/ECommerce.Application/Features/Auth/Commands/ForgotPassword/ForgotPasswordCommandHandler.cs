using ECommerce.Application.Common.Interfaces;
using MediatR;

namespace ECommerce.Application.Features.Auth.Commands.ForgotPassword;

public sealed class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand>
{
    private readonly IAccountSecurityService _securityService;

    public ForgotPasswordCommandHandler(IAccountSecurityService securityService) => _securityService = securityService;

    public Task Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
        => _securityService.RequestPasswordResetAsync(request.Email, cancellationToken);
}
