using ECommerce.Application.Common.Interfaces;
using MediatR;
namespace ECommerce.Application.Features.Auth.Commands.ConfirmEmailChange;
public sealed class ConfirmEmailChangeCommandHandler : IRequestHandler<ConfirmEmailChangeCommand>
{
    private readonly IAccountSecurityService _service;
    public ConfirmEmailChangeCommandHandler(IAccountSecurityService service) => _service = service;
    public Task Handle(ConfirmEmailChangeCommand request, CancellationToken cancellationToken)
        => _service.ConfirmEmailChangeAsync(request.UserId, request.NewEmail, request.Token, cancellationToken);
}
