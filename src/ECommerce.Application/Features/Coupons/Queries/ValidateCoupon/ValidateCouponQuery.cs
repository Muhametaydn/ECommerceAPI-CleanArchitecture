using ECommerce.Application.Features.Coupons.DTOs;
using MediatR;

namespace ECommerce.Application.Features.Coupons.Queries.ValidateCoupon
{
    /// <summary>
    /// Kupon kodunu doğrular ve indirim tutarını hesaplar.
    /// Müşteri checkout sırasında kupon kodunu girer ve sonucu görür.
    /// </summary>
    public class ValidateCouponQuery : IRequest<CouponValidationDTO>
    {
        public string Code { get; set; } = string.Empty;
        public decimal OrderTotal { get; set; }
    }
}
