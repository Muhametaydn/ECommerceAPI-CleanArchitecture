using ECommerce.Application.Common.Interfaces;
using MediatR;

namespace ECommerce.Application.Features.Auth.Commands.ConfirmEmail;

public sealed class ConfirmEmailCommandHandler : IRequestHandler<ConfirmEmailCommand>
{
    private readonly IAccountSecurityService _securityService;

    public ConfirmEmailCommandHandler(IAccountSecurityService securityService) => _securityService = securityService;

    public Task Handle(ConfirmEmailCommand request, CancellationToken cancellationToken)
        => _securityService.ConfirmEmailAsync(request.Email, request.Token, cancellationToken);
}
