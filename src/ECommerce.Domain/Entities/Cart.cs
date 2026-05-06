namespace ECommerce.Domain.Entities
{
    /// <summary>
    /// Redis'te JSON olarak saklanan sepet entity'si. Veritabanı entity'si değildir.
    /// </summary>
    public class Cart
    {
        public string Id { get; set; } = string.Empty;
        public string? UserId { get; set; }
        public List<CartItem> Items { get; set; } = new();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Hesaplanmış toplam fiyat
        public decimal TotalPrice => Items.Sum(i => i.SubTotal);

        // Sepetteki toplam ürün adedi
        public int TotalItems => Items.Sum(i => i.Quantity);

        /// <summary>
        /// Sepete ürün ekler. Aynı üründen varsa miktarı artırır.
        /// </summary>
        public void AddItem(Guid productId, string productName, decimal unitPrice, int quantity)
        {
            var existingItem = Items.FirstOrDefault(i => i.ProductId == productId);

            if (existingItem is not null)
            {
                existingItem.Quantity += quantity;
            }
            else
            {
                Items.Add(new CartItem
                {
                    ProductId = productId,
                    ProductName = productName,
                    UnitPrice = unitPrice,
                    Quantity = quantity
                });
            }

            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Sepetten ürün kaldırır.
        /// </summary>
        public void RemoveItem(Guid productId)
        {
            Items.RemoveAll(i => i.ProductId == productId);
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Ürün miktarını günceller. Miktar 0 veya altındaysa ürünü kaldırır.
        /// </summary>
        public void UpdateQuantity(Guid productId, int quantity)
        {
            if (quantity <= 0)
            {
                RemoveItem(productId);
                return;
            }

            var item = Items.FirstOrDefault(i => i.ProductId == productId);
            if (item is not null)
            {
                item.Quantity = quantity;
                UpdatedAt = DateTime.UtcNow;
            }
        }

        /// <summary>
        /// Sepeti tamamen temizler.
        /// </summary>
        public void Clear()
        {
            Items.Clear();
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public class CartItem
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }

        // Kalem toplam tutarı
        public decimal SubTotal => UnitPrice * Quantity;
    }
}
