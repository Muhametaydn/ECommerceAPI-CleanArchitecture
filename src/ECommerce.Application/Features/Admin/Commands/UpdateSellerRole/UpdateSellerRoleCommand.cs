using ECommerce.Application.Features.Admin.DTOs;
using MediatR;

namespace ECommerce.Application.Features.Admin.Commands.UpdateSellerRole
{
    public record UpdateSellerRoleCommand(Guid UserId, bool IsSeller) : IRequest<AdminUserDTO>;
}
