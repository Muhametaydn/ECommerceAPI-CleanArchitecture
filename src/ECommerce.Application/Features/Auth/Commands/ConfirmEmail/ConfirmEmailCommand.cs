using MediatR;

namespace ECommerce.Application.Features.Auth.Commands.ConfirmEmail;

public sealed record ConfirmEmailCommand(string Email, string Token) : IRequest;
