using ECommerce.Application.Common.Exceptions;
using ECommerce.Domain.Interfaces;
using MediatR;

namespace ECommerce.Application.Features.Coupons.Commands.DeleteCoupon
{
    public class DeleteCouponCommandHandler : IRequestHandler<DeleteCouponCommand, Unit>
    {
        private readonly IUnitOfWork _unitOfWork;

        public DeleteCouponCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Unit> Handle(DeleteCouponCommand request, CancellationToken cancellationToken)
        {
            var coupon = await _unitOfWork.Coupon.GetByIdAsync(request.Id)
                ?? throw new NotFoundException("Kupon", request.Id);

            // Soft delete - deaktif et
            coupon.Deactivate();
            _unitOfWork.Coupon.Update(coupon);
            await _unitOfWork.SaveChangesAsync();

            return Unit.Value;
        }
    }
}
