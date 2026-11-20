using ECommerce.Application.Features.Admin.DTOs;

namespace ECommerce.Application.Common.Interfaces
{
    public interface IAdminQueryService
    {
        Task<IReadOnlyList<AdminUserDTO>> GetUsersAsync(CancellationToken cancellationToken = default);
        Task<AdminDashboardStatsDTO> GetDashboardStatsAsync(CancellationToken cancellationToken = default);
    }
}
