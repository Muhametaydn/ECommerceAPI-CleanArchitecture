using AutoMapper;
using ECommerce.Application.Features.Orders.DTOs;
using ECommerce.Domain.Entities;

namespace ECommerce.Application.Features.Orders.Mappings
{
    public class OrderMappingProfile : Profile
    {
        public OrderMappingProfile()
        {
            // Order → OrderDTO (liste görünümü)
            CreateMap<Order, OrderDTO>()
                .ForMember(dest => dest.TotalItems,
                    opt => opt.MapFrom(src => src.OrderItems.Sum(oi => oi.Quantity)));

            // Order → OrderDetailDTO (detay görünümü)
            CreateMap<Order, OrderDetailDTO>()
                .ForMember(dest => dest.ShippingAddress,
                    opt => opt.MapFrom(src => src.ShippingAddress.AddressLine))
                .ForMember(dest => dest.ShippingCity,
                    opt => opt.MapFrom(src => src.ShippingAddress.City))
                .ForMember(dest => dest.ShippingDistrict,
                    opt => opt.MapFrom(src => src.ShippingAddress.District))
                .ForMember(dest => dest.PaymentStatus,
                    opt => opt.MapFrom(src => src.Payment != null ? src.Payment.Status : (Domain.Enums.PaymentStatus?)null))
                .ForMember(dest => dest.PaymentMethod,
                    opt => opt.MapFrom(src => src.Payment != null ? src.Payment.Method : (Domain.Enums.PaymentMethod?)null))
                .ForMember(dest => dest.Items,
                    opt => opt.MapFrom(src => src.OrderItems))
                .ForMember(dest => dest.UpdatedAt,
                    opt => opt.MapFrom(src => src.UpdateAt));

            // OrderItem → OrderItemDTO
            CreateMap<OrderItem, OrderItemDTO>()
                .ForMember(dest => dest.ProductName,
                    opt => opt.MapFrom(src => src.Product.Name));
        }
    }
}
