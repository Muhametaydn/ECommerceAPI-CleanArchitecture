using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Domain.Entities
{
    public class Coupon : Common.BaseEntity
    {
        public string Code { get; set; } = string.Empty;
        public Enums.DiscountType DiscountType { get; set; }
        public decimal DiscountValue { get; set; }
        public decimal? MinimumOrderAmount { get; set; }
        public int MaxUsageCount { get; set; }
        public int CurrentUsageCount { get; set; }
        public DateTime ExpiryDate { get; set; }


        public bool IsValid() 
        {
            return DateTime.UtcNow <= ExpiryDate
                && CurrentUsageCount < MaxUsageCount;
        }

        public decimal CalculateDiscount(decimal orderTotal)
        {
            if (!IsValid())
                throw new InvalidOperationException($"Minimum sipariş tutarı{MinimumOrderAmount.Value} TL");

            return DiscountType == Enums.DiscountType.Percentage ? orderTotal * (DiscountValue / 100) : DiscountValue;
        }

        public void Use() {
            if (!IsValid())
                throw new InvalidOperationException("Kupon geçersiz.");

            CurrentUsageCount++;
            UpdateAt = DateTime.UtcNow;
        }




    }
}
