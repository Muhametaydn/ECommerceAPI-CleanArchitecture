namespace ECommerce.Application.Common.Interfaces;

public interface IAccountSecurityService
{
    Task ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken cancellationToken = default);
    Task RequestPasswordResetAsync(string email, CancellationToken cancellationToken = default);
    Task ResetPasswordAsync(string email, string token, string newPassword, CancellationToken cancellationToken = default);
    Task RequestEmailConfirmationAsync(string email, CancellationToken cancellationToken = default);
    Task RequestEmailChangeAsync(Guid userId, string newEmail, string currentPassword, CancellationToken cancellationToken = default);
    Task ConfirmEmailChangeAsync(Guid userId, string newEmail, string token, CancellationToken cancellationToken = default);
    Task ConfirmEmailAsync(string email, string token, CancellationToken cancellationToken = default);
}
