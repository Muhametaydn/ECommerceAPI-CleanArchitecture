using MediatR;
namespace ECommerce.Application.Features.Auth.Commands.ConfirmEmailChange;
public sealed record ConfirmEmailChangeCommand(Guid UserId, string NewEmail, string Token) : IRequest;
