using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Features.Admin.DTOs;
using MediatR;

namespace ECommerce.Application.Features.Admin.Queries.GetAdminDashboard
{
    public class GetAdminDashboardQueryHandler : IRequestHandler<GetAdminDashboardQuery, AdminDashboardStatsDTO>
    {
        private readonly IAdminQueryService _adminQueryService;

        public GetAdminDashboardQueryHandler(IAdminQueryService adminQueryService)
        {
            _adminQueryService = adminQueryService;
        }

        public Task<AdminDashboardStatsDTO> Handle(
            GetAdminDashboardQuery request,
            CancellationToken cancellationToken)
            => _adminQueryService.GetDashboardStatsAsync(cancellationToken);
    }
}
