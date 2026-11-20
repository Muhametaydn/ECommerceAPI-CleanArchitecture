using ECommerce.Application.Features.Admin.DTOs;
using MediatR;

namespace ECommerce.Application.Features.Admin.Queries.GetAdminUsers
{
    public record GetAdminUsersQuery : IRequest<IReadOnlyList<AdminUserDTO>>;
}
