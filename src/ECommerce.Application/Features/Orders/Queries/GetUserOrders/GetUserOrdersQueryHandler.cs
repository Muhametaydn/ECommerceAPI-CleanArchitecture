using AutoMapper;
using ECommerce.Application.Features.Orders.DTOs;
using ECommerce.Domain.Interfaces;
using MediatR;

namespace ECommerce.Application.Features.Orders.Queries.GetUserOrders
{
    public class GetUserOrdersQueryHandler : IRequestHandler<GetUserOrdersQuery, IReadOnlyList<OrderDTO>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public GetUserOrdersQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<IReadOnlyList<OrderDTO>> Handle(GetUserOrdersQuery request, CancellationToken cancellationToken)
        {
            var orders = await _unitOfWork.Order.GetOrdersByUserAsync(request.UserId);
            return _mapper.Map<IReadOnlyList<OrderDTO>>(orders);
        }
    }
}
