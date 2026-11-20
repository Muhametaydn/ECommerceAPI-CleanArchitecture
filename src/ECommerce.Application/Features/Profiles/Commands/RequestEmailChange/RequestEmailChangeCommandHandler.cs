using ECommerce.Application.Common.Interfaces;
using MediatR;
namespace ECommerce.Application.Features.Profiles.Commands.RequestEmailChange;
public sealed class RequestEmailChangeCommandHandler : IRequestHandler<RequestEmailChangeCommand>
{
    private readonly IAccountSecurityService _service;
    public RequestEmailChangeCommandHandler(IAccountSecurityService service) => _service = service;
    public Task Handle(RequestEmailChangeCommand request, CancellationToken cancellationToken)
        => _service.RequestEmailChangeAsync(request.UserId, request.NewEmail, request.CurrentPassword, cancellationToken);
}
