using AutoMapper;
using ECommerce.Application.Features.Coupons.DTOs;
using ECommerce.Domain.Entities;

namespace ECommerce.Application.Features.Coupons.Mappings
{
    public class CouponMappingProfile : Profile
    {
        public CouponMappingProfile()
        {
            CreateMap<Coupon, CouponDTO>()
                .ForMember(dest => dest.IsValid,
                    opt => opt.MapFrom(src => src.IsValid()));
        }
    }
}
