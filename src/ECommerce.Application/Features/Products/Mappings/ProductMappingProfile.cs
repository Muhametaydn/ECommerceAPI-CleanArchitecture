using AutoMapper;
using ECommerce.Application.Features.Products.DTOs;
using ECommerce.Domain.Entities;

namespace ECommerce.Application.Features.Products.Mappings
{
    public class ProductMappingProfile : Profile
    {
        public ProductMappingProfile()
        {
            CreateMap<Product, ProductDTO>()
                .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Category.Name));

            // Elasticsearch document → ProductDTO (search sonuçları için)
            CreateMap<ProductSearchDocument, ProductDTO>()
                .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.CategoryName));

            // Product entity → Elasticsearch document (indexleme için)
            CreateMap<Product, ProductSearchDocument>()
                .ForMember(dest => dest.CategoryName,
                    opt => opt.MapFrom(src => src.Category != null ? src.Category.Name : string.Empty));
        }
    }
}
