using ECommerce.Application.Features.Admin.DTOs;

namespace ECommerce.Application.Common.Interfaces
{
    public interface IUserRoleManagementService
    {
        Task<AdminUserDTO> SetSellerRoleAsync(
            Guid userId,
            bool isSeller,
            CancellationToken cancellationToken = default);
    }
}
