using ECommerce.Domain.Events.Products;

namespace ECommerce.Domain.Entities
{
    public class Product : Common.BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public string SKU { get; set; } = string.Empty ;
        public string? ImageUrl { get; private set; }
        public bool IsActive { get; set; } = true;
        public Enums.ProductApprovalStatus ApprovalStatus { get; private set; } = Enums.ProductApprovalStatus.Approved;

        public Guid? SellerId { get; private set; }
        public ApplicationUser? Seller { get; private set; }

        public Guid CategoryId { get; set; }
        public Category Category { get; set; } = null!;

        public ICollection<Review> Reviews { get; set; } = new List<Review>();

        public void AssignSeller(Guid sellerId, bool autoApprove)
        {
            SellerId = sellerId;
            ApprovalStatus = autoApprove
                ? Enums.ProductApprovalStatus.Approved
                : Enums.ProductApprovalStatus.Pending;
            IsActive = autoApprove;
        }

        public void RequireApproval()
        {
            ApprovalStatus = Enums.ProductApprovalStatus.Pending;
            IsActive = false;
            UpdateAt = DateTime.UtcNow;
        }

        public void Approve()
        {
            ApprovalStatus = Enums.ProductApprovalStatus.Approved;
            IsActive = true;
            UpdateAt = DateTime.UtcNow;
        }

        public void Reject()
        {
            ApprovalStatus = Enums.ProductApprovalStatus.Rejected;
            IsActive = false;
            UpdateAt = DateTime.UtcNow;
        }

        public void UpdatePrice(decimal newPrice) {

            if (newPrice < 0)
            {
                throw new ArgumentException("Fiyat negatif olamaz. ");
            }

            Price = newPrice;
            UpdateAt = DateTime.UtcNow;
        }

        public void UpdateImage(string? imageUrl)
        {
            ImageUrl = string.IsNullOrWhiteSpace(imageUrl) ? null : imageUrl.Trim();
            UpdateAt = DateTime.UtcNow;
        }

        public void DecreaseStock(int quantity)
        {
            if (quantity <= 0)
                throw new ArgumentException("Miktar pozitif olmalı.");

            if (StockQuantity < quantity)
                throw new ArgumentException("Yetersiz stok.");

            int previousStock = StockQuantity;
            StockQuantity -= quantity;
            UpdateAt = DateTime.UtcNow;

            AddDomainEvent(new StockDecreasedDomainEvent(Id, Name, previousStock, StockQuantity, quantity));
        }

        public void IncreaseStock(int quantity) {

            if (quantity <= 0)
                throw new ArgumentException("Miktar pozitif olmalı.");

            StockQuantity += quantity;
            UpdateAt = DateTime.UtcNow;
        }

    }
}
