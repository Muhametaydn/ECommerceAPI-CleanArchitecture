using AutoMapper;
using ECommerce.Application.Features.Addresses.DTOs;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Interfaces;
using MediatR;

namespace ECommerce.Application.Features.Addresses.Commands.CreateAddress
{
    public class CreateAddressCommandHandler : IRequestHandler<CreateAddressCommand, AddressDTO>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public CreateAddressCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<AddressDTO> Handle(CreateAddressCommand request, CancellationToken cancellationToken)
        {
            var address = new Address
            {
                Id = Guid.NewGuid(),
                Tittle = request.Title,
                AddressLine = request.AddressLine,
                City = request.City,
                District = request.District,
                PostalCode = request.PostalCode,
                Country = request.Country,
                UserId = request.UserId
            };

            await _unitOfWork.Address.AddAsync(address);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<AddressDTO>(address);
        }
    }
}
