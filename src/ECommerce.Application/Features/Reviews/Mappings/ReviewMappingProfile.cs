using AutoMapper;
using ECommerce.Application.Features.Reviews.DTOs;
using ECommerce.Domain.Entities;

namespace ECommerce.Application.Features.Reviews.Mappings
{
    public class ReviewMappingProfile : Profile
    {
        public ReviewMappingProfile()
        {
            CreateMap<Review, ReviewDTO>()
                .ForMember(dest => dest.ProductName,
                    opt => opt.MapFrom(src => src.Product != null ? src.Product.Name : string.Empty))
                .ForMember(dest => dest.UserName,
                    opt => opt.MapFrom(src => src.User != null ? src.User.FirstName + " " + src.User.LastName : string.Empty));
        }
    }
}
