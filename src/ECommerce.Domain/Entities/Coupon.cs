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
        public bool IsActive { get; set; } = true;


        public bool IsValid()
        {
            return IsActive
                && DateTime.UtcNow <= ExpiryDate
                && CurrentUsageCount < MaxUsageCount;
        }

        public decimal CalculateDiscount(decimal orderTotal)
        {
            if (!IsValid())
                throw new InvalidOperationException("Kupon geçersiz veya süresi dolmuş.");

            if (MinimumOrderAmount.HasValue && orderTotal < MinimumOrderAmount.Value)
                throw new InvalidOperationException(
                    $"Bu kuponu kullanmak için minimum sipariş tutarı {MinimumOrderAmount.Value:N2} TL olmalıdır.");

            var discount = DiscountType == Enums.DiscountType.Percentage
                ? orderTotal * (DiscountValue / 100)
                : DiscountValue;

            // İndirim sipariş tutarını aşamaz
            return Math.Min(discount, orderTotal);
        }

        public void Use() {
            if (!IsValid())
                throw new InvalidOperationException("Kupon geçersiz.");

            CurrentUsageCount++;
            UpdateAt = DateTime.UtcNow;
        }

        public void Deactivate()
        {
            IsActive = false;
            UpdateAt = DateTime.UtcNow;
        }




    }
}
