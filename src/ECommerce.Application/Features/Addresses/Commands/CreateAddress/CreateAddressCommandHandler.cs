using AutoMapper;
using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
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
        private readonly IUserLookupService _userLookupService;

        public CreateAddressCommandHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IUserLookupService userLookupService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _userLookupService = userLookupService;
        }

        public async Task<AddressDTO> Handle(CreateAddressCommand request, CancellationToken cancellationToken)
        {
            if (!await _userLookupService.ExistsAsync(request.UserId, cancellationToken))
                throw new NotFoundException("Kullanıcı", request.UserId);

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
