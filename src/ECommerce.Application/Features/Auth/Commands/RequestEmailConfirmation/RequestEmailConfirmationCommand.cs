using MediatR;

namespace ECommerce.Application.Features.Auth.Commands.RequestEmailConfirmation;

public sealed record RequestEmailConfirmationCommand(string Email) : IRequest;
