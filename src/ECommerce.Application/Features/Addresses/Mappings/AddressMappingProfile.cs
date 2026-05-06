using AutoMapper;
using ECommerce.Application.Features.Addresses.DTOs;
using ECommerce.Domain.Entities;

namespace ECommerce.Application.Features.Addresses.Mappings
{
    public class AddressMappingProfile : Profile
    {
        public AddressMappingProfile()
        {
            CreateMap<Address, AddressDTO>()
                .ForMember(dest => dest.Title,
                    opt => opt.MapFrom(src => src.Tittle)); // Domain'deki typo korunuyor
        }
    }
}
