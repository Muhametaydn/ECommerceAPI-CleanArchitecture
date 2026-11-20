using ECommerce.Application.Common.Interfaces;
using MediatR;

namespace ECommerce.Application.Features.Auth.Commands.RequestEmailConfirmation;

public sealed class RequestEmailConfirmationCommandHandler : IRequestHandler<RequestEmailConfirmationCommand>
{
    private readonly IAccountSecurityService _securityService;

    public RequestEmailConfirmationCommandHandler(IAccountSecurityService securityService) => _securityService = securityService;

    public Task Handle(RequestEmailConfirmationCommand request, CancellationToken cancellationToken)
        => _securityService.RequestEmailConfirmationAsync(request.Email, cancellationToken);
}
