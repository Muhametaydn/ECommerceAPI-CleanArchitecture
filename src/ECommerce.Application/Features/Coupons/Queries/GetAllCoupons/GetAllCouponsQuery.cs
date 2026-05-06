using ECommerce.Application.Features.Coupons.DTOs;
using MediatR;

namespace ECommerce.Application.Features.Coupons.Queries.GetAllCoupons
{
    /// <summary>
    /// Tüm kuponları getirir (Admin)
    /// </summary>
    public record GetAllCouponsQuery(bool IncludeInactive = false) : IRequest<IReadOnlyList<CouponDTO>>;
}
