using AutoMapper;
using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Features.Addresses.DTOs;
using ECommerce.Domain.Interfaces;
using MediatR;

namespace ECommerce.Application.Features.Addresses.Commands.UpdateAddress
{
    public class UpdateAddressCommandHandler : IRequestHandler<UpdateAddressCommand, AddressDTO>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public UpdateAddressCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<AddressDTO> Handle(UpdateAddressCommand request, CancellationToken cancellationToken)
        {
            var address = await _unitOfWork.Address.GetByIdAsync(request.Id)
                ?? throw new NotFoundException("Adres", request.Id);

            if (address.UserId != request.UserId)
                throw new UnauthorizedAccessException("Bu adresi güncelleme yetkiniz yok.");

            address.Tittle = request.Title;
            address.AddressLine = request.AddressLine;
            address.City = request.City;
            address.District = request.District;
            address.PostalCode = request.PostalCode;
            address.Country = request.Country;
            address.UpdateAt = DateTime.UtcNow;

            _unitOfWork.Address.Update(address);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<AddressDTO>(address);
        }
    }
}
