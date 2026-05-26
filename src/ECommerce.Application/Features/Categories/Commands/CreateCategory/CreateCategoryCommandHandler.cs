using ECommerce.Application.Common.Interfaces;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Interfaces;
using MediatR;

namespace ECommerce.Application.Features.Categories.Commands.CreateCategory;

public class CreateCategoryCommandHandler : IRequestHandler<CreateCategoryCommand, Guid>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cacheService;

    public CreateCategoryCommandHandler(IUnitOfWork unitOfWork, ICacheService cacheService)
    {
        _unitOfWork = unitOfWork;
        _cacheService = cacheService;
    }

    public async Task<Guid> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        // Slug olustur ve benzersizligini kontrol et
        var slug = Category.GenerateSlug(request.Name);
        if (await _unitOfWork.Category.SlugExistsAsync(slug))
        {
            var counter = 2;
            while (await _unitOfWork.Category.SlugExistsAsync($"{slug}-{counter}"))
                counter++;
            slug = $"{slug}-{counter}";
        }

        var category = new Category
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
            Slug = slug,
            SortOrder = request.SortOrder
        };

        // Ust kategori varsa derinlik kontrolu yap
        if (request.ParentCategoryId.HasValue)
        {
            var parent = await _unitOfWork.Category.GetByIdAsync(request.ParentCategoryId.Value);
            if (parent == null)
                throw new InvalidOperationException("Ust kategori bulunamadi.");

            category.SetParent(parent);
        }

        await _unitOfWork.Category.AddAsync(category);
        await _unitOfWork.SaveChangesAsync();

        // Tüm kategori cache'lerini temizle
        await _cacheService.RemoveByPrefixAsync("categories:", cancellationToken);

        return category.Id;
    }
}
