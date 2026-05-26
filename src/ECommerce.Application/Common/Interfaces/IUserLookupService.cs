namespace ECommerce.Application.Common.Interfaces
{
    /// <summary>
    /// Kullanıcı bilgisi lookup servisi.
    /// Consumer'ların UserId'den e-posta ve isim almasını sağlar.
    /// </summary>
    public interface IUserLookupService
    {
        Task<(string Email, string FullName)?> GetUserInfoAsync(Guid userId, CancellationToken ct = default);
    }
}
