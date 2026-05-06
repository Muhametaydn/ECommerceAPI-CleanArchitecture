using AutoMapper;
using ECommerce.Application.Features.Addresses.DTOs;
using ECommerce.Domain.Interfaces;
using MediatR;

namespace ECommerce.Application.Features.Addresses.Queries.GetUserAddresses
{
    public class GetUserAddressesQueryHandler : IRequestHandler<GetUserAddressesQuery, IReadOnlyList<AddressDTO>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public GetUserAddressesQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<IReadOnlyList<AddressDTO>> Handle(GetUserAddressesQuery request, CancellationToken cancellationToken)
        {
            var addresses = await _unitOfWork.Address.GetByUserIdAsync(request.UserId);
            return _mapper.Map<IReadOnlyList<AddressDTO>>(addresses);
        }
    }
}
