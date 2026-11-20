namespace ECommerce.Domain.Entities
{
    public class MarketplaceSettings : Common.BaseEntity
    {
        public static readonly Guid DefaultId = Guid.Parse("d4444444-4444-4444-4444-444444444444");

        public bool AutoApproveSellerProducts { get; private set; }

        public void SetAutoApproval(bool enabled)
        {
            AutoApproveSellerProducts = enabled;
            UpdateAt = DateTime.UtcNow;
        }
    }
}
