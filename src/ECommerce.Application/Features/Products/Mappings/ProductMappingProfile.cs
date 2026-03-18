using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using ECommerce.Application.Features.Products.DTOs;
using ECommerce.Domain.Entities;
using ECommerce.Application.Features.Products.DTOs;
using ECommerce.Domain.Entities;


namespace ECommerce.Application.Features.Products.Mappings
{
    public class ProductMappingProfile : Profile
    {
        public ProductMappingProfile() {
            CreateMap<Product, ProductDTO>()
            .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Category.Name));



        }




    }
}
