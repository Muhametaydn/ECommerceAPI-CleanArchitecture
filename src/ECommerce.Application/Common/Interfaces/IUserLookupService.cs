namespace ECommerce.Application.Common.Interfaces
{
    /// <summary>
    /// Kullanıcı bilgisi lookup servisi.
    /// Consumer'ların UserId'den e-posta ve isim almasını sağlar.
    /// </summary>
    public interface IUserLookupService
    {
        Task<bool> ExistsAsync(Guid userId, CancellationToken ct = default);
        Task<(string Email, string FullName)?> GetUserInfoAsync(Guid userId, CancellationToken ct = default);

        /// <summary>Fraud değerlendirmesi için hesap bilgileri (hesap yaşı, e-posta, doğrulama durumu).</summary>
        Task<UserRiskProfile?> GetRiskProfileAsync(Guid userId, CancellationToken ct = default);
    }

    public sealed record UserRiskProfile(DateTime AccountCreatedAt, string Email, bool EmailConfirmed);
}
