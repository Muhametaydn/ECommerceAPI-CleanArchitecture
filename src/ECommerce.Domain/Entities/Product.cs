using ECommerce.Domain.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Domain.Entities
{
    public class Product : Common.BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public string SKU { get; set; } = string.Empty ;
        public bool IsActive { get; set; } = true;

        public Guid CategoryId { get; set; }
        public Category Category { get; set; } = null!;

        public ICollection<Review> Reviews { get; set; } = new List<Review>();

        public void UpdatePrice(decimal newPrice) {

            if (newPrice < 0)
            {
                throw new ArgumentException("Fiyat negatif olamaz. ");
            }

            Price = newPrice;
            UpdateAt = DateTime.UtcNow;
        }

        public void DecreaseStock(int quantity) {

            if (quantity <= 0)
                throw new ArgumentException("Miktar pozitif olmalı.");

            if (StockQuantity < quantity)
                throw new ArgumentException("Yetersiz stok.");


            StockQuantity -= quantity;
            UpdateAt = DateTime.UtcNow;
            

        }

    }
}
