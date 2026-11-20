using ECommerce.Application.Features.Admin.DTOs;
using MediatR;

namespace ECommerce.Application.Features.Admin.Queries.GetAdminDashboard
{
    public record GetAdminDashboardQuery : IRequest<AdminDashboardStatsDTO>;
}
