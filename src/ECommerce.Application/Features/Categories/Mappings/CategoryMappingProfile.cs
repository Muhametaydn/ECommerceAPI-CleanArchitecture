using AutoMapper;
using ECommerce.Application.Features.Categories.DTOs;
using ECommerce.Domain.Entities;

namespace ECommerce.Application.Features.Categories.Mappings;

public class CategoryMappingProfile : Profile
{
    public CategoryMappingProfile()
    {
        CreateMap<Category, CategoryDTO>()
            .ForMember(dest => dest.ParentCategoryName,
                opt => opt.MapFrom(src => src.ParentCategory != null ? src.ParentCategory.Name : null))
            .ForMember(dest => dest.SubCategoryCount,
                opt => opt.MapFrom(src => src.SubCategories.Count))
            .ForMember(dest => dest.ProductCount,
                opt => opt.MapFrom(src => src.Products.Count));

        CreateMap<Category, CategoryTreeDTO>()
            .ForMember(dest => dest.SubCategories,
                opt => opt.MapFrom(src => src.SubCategories))
            .ForMember(dest => dest.ProductCount,
                opt => opt.MapFrom(src => src.Products.Count));

        CreateMap<Category, BreadcrumbItem>();
    }
}
