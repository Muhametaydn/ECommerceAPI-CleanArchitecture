using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Interfaces;
using MediatR;

namespace ECommerce.Application.Features.Categories.Commands.UpdateCategory;

public class UpdateCategoryCommandHandler : IRequestHandler<UpdateCategoryCommand>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cacheService;

    public UpdateCategoryCommandHandler(IUnitOfWork unitOfWork, ICacheService cacheService)
    {
        _unitOfWork = unitOfWork;
        _cacheService = cacheService;
    }

    public async Task Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await _unitOfWork.Category.GetByIdAsync(request.Id);
        if (category == null)
            throw new NotFoundException(nameof(Category), request.Id);

        // Isim degistiyse slug'i da guncelle
        if (category.Name != request.Name)
        {
            var newSlug = Category.GenerateSlug(request.Name);
            if (await _unitOfWork.Category.SlugExistsAsync(newSlug, excludeCategoryId: category.Id))
            {
                var counter = 2;
                while (await _unitOfWork.Category.SlugExistsAsync($"{newSlug}-{counter}", category.Id))
                    counter++;
                newSlug = $"{newSlug}-{counter}";
            }
            category.Slug = newSlug;
        }

        category.Name = request.Name;
        category.Description = request.Description;
        category.SortOrder = request.SortOrder;

        if (request.IsActive && !category.IsActive)
            category.Activate();
        else if (!request.IsActive && category.IsActive)
            category.Deactivate();

        _unitOfWork.Category.Update(category);
        await _unitOfWork.SaveChangesAsync();

        // Tüm kategori cache'lerini temizle
        await _cacheService.RemoveByPrefixAsync("categories:", cancellationToken);
    }
}
