using ECommerce.Application.Common.Exceptions;
using ECommerce.Domain.Interfaces;
using MediatR;

namespace ECommerce.Application.Features.Addresses.Commands.DeleteAddress
{
    public class DeleteAddressCommandHandler : IRequestHandler<DeleteAddressCommand, Unit>
    {
        private readonly IUnitOfWork _unitOfWork;

        public DeleteAddressCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Unit> Handle(DeleteAddressCommand request, CancellationToken cancellationToken)
        {
            var address = await _unitOfWork.Address.GetByIdAsync(request.Id)
                ?? throw new NotFoundException("Adres", request.Id);

            if (address.UserId != request.UserId)
                throw new UnauthorizedAccessException("Bu adresi silme yetkiniz yok.");

            // Aktif siparişlerde kullanılıyor mu kontrol et
            var hasActiveOrders = await _unitOfWork.Order
                .AnyAsync(o => o.ShippingAddressId == request.Id
                    && o.Status != Domain.Enums.OrderStatus.Delivered
                    && o.Status != Domain.Enums.OrderStatus.Cancelled);

            if (hasActiveOrders)
                throw new InvalidOperationException(
                    "Bu adres aktif siparişlerde kullanılıyor. Siparişler tamamlanmadan silinemez.");

            _unitOfWork.Address.Delete(address);
            await _unitOfWork.SaveChangesAsync();

            return Unit.Value;
        }
    }
}
