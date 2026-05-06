using AutoMapper;
using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Features.Payments.DTOs;
using ECommerce.Domain.Interfaces;
using MediatR;

namespace ECommerce.Application.Features.Payments.Queries.GetPaymentByOrderId
{
    public class GetPaymentByOrderIdQueryHandler : IRequestHandler<GetPaymentByOrderIdQuery, PaymentDTO>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public GetPaymentByOrderIdQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<PaymentDTO> Handle(GetPaymentByOrderIdQuery request, CancellationToken cancellationToken)
        {
            var order = await _unitOfWork.Order.GetByIdAsync(request.OrderId)
                ?? throw new NotFoundException("Sipariş", request.OrderId);

            // Yetki kontrolü
            if (!request.IsAdmin && order.UserId != request.UserId)
                throw new UnauthorizedAccessException("Bu siparişin ödeme bilgisini görüntüleme yetkiniz yok.");

            var payment = await _unitOfWork.Payment.GetByOrderIdAsync(request.OrderId)
                ?? throw new NotFoundException("Ödeme", request.OrderId);

            return _mapper.Map<PaymentDTO>(payment);
        }
    }
}
