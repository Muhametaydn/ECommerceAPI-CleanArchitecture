using AutoMapper;
using ECommerce.Application.Features.Coupons.DTOs;
using ECommerce.Domain.Interfaces;
using MediatR;

namespace ECommerce.Application.Features.Coupons.Queries.GetAllCoupons
{
    public class GetAllCouponsQueryHandler : IRequestHandler<GetAllCouponsQuery, IReadOnlyList<CouponDTO>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public GetAllCouponsQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<IReadOnlyList<CouponDTO>> Handle(GetAllCouponsQuery request, CancellationToken cancellationToken)
        {
            var coupons = request.IncludeInactive
                ? await _unitOfWork.Coupon.GetAllAsync()
                : await _unitOfWork.Coupon.GetWhereAsync(c => c.IsActive);

            return _mapper.Map<IReadOnlyList<CouponDTO>>(coupons);
        }
    }
}
