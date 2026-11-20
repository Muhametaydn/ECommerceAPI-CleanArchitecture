using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Features.Admin.DTOs;
using ECommerce.Domain.Constants;
using ECommerce.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace ECommerce.Infrastructure.Services
{
    public class UserRoleManagementService : IUserRoleManagementService
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public UserRoleManagementService(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<AdminUserDTO> SetSellerRoleAsync(
            Guid userId,
            bool isSeller,
            CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString())
                ?? throw new KeyNotFoundException("Kullanıcı bulunamadı.");

            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Contains(AppRoles.Admin))
                throw new InvalidOperationException("Yönetici hesabının rolü bu ekrandan değiştirilemez.");

            var roleToAdd = isSeller ? AppRoles.Seller : AppRoles.Customer;
            var roleToRemove = isSeller ? AppRoles.Customer : AppRoles.Seller;

            if (roles.Contains(roleToRemove))
            {
                var removeResult = await _userManager.RemoveFromRoleAsync(user, roleToRemove);
                if (!removeResult.Succeeded)
                    throw new InvalidOperationException(string.Join(" ", removeResult.Errors.Select(error => error.Description)));
            }

            if (!roles.Contains(roleToAdd))
            {
                var addResult = await _userManager.AddToRoleAsync(user, roleToAdd);
                if (!addResult.Succeeded)
                    throw new InvalidOperationException(string.Join(" ", addResult.Errors.Select(error => error.Description)));
            }

            var updatedRoles = await _userManager.GetRolesAsync(user);
            return new AdminUserDTO
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email ?? string.Empty,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                Roles = updatedRoles.ToArray()
            };
        }
    }
}
