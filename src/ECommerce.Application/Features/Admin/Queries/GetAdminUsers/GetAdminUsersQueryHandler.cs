using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Features.Admin.DTOs;
using MediatR;

namespace ECommerce.Application.Features.Admin.Queries.GetAdminUsers
{
    public class GetAdminUsersQueryHandler : IRequestHandler<GetAdminUsersQuery, IReadOnlyList<AdminUserDTO>>
    {
        private readonly IAdminQueryService _adminQueryService;

        public GetAdminUsersQueryHandler(IAdminQueryService adminQueryService)
        {
            _adminQueryService = adminQueryService;
        }

        public Task<IReadOnlyList<AdminUserDTO>> Handle(
            GetAdminUsersQuery request,
            CancellationToken cancellationToken)
            => _adminQueryService.GetUsersAsync(cancellationToken);
    }
}
