using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Features.Admin.DTOs;
using MediatR;

namespace ECommerce.Application.Features.Admin.Commands.UpdateSellerRole
{
    public class UpdateSellerRoleCommandHandler
        : IRequestHandler<UpdateSellerRoleCommand, AdminUserDTO>
    {
        private readonly IUserRoleManagementService _roleManagement;

        public UpdateSellerRoleCommandHandler(IUserRoleManagementService roleManagement)
        {
            _roleManagement = roleManagement;
        }

        public Task<AdminUserDTO> Handle(
            UpdateSellerRoleCommand request,
            CancellationToken cancellationToken)
            => _roleManagement.SetSellerRoleAsync(
                request.UserId,
                request.IsSeller,
                cancellationToken);
    }
}
