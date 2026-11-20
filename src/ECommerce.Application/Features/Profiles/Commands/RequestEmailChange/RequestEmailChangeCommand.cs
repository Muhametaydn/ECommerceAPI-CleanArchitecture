using MediatR;
namespace ECommerce.Application.Features.Profiles.Commands.RequestEmailChange;
public sealed record RequestEmailChangeCommand(Guid UserId, string NewEmail, string CurrentPassword) : IRequest;
