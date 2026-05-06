using ECommerce.Domain.Entities;

namespace ECommerce.Application.Interfaces
{
    public interface ICartService
    {
        Task<Cart?> GetCartAsync(string cartId);
        Task SaveCartAsync(Cart cart);
        Task DeleteCartAsync(string cartId);

        /// <summary>
        /// Anonim sepeti kullanıcı sepetine birleştirir.
        /// Aynı üründen varsa miktarları toplanır.
        /// </summary>
        Task<Cart> MergeCartsAsync(string anonymousCartId, string userCartId);
    }
}
