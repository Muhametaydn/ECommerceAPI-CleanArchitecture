using AutoMapper;
using ECommerce.Application.Features.Payments.DTOs;
using ECommerce.Domain.Entities;

namespace ECommerce.Application.Features.Payments.Mappings
{
    public class PaymentMappingProfile : Profile
    {
        public PaymentMappingProfile()
        {
            CreateMap<Payment, PaymentDTO>()
                .ForMember(dest => dest.OrderNumber,
                    opt => opt.MapFrom(src => src.Order != null ? src.Order.OrderNumber : string.Empty))
                .ForMember(dest => dest.TransactionId,
                    opt => opt.MapFrom(src => src.TransacionId))
                .ForMember(dest => dest.UpdatedAt,
                    opt => opt.MapFrom(src => src.UpdateAt));
        }
    }
}
