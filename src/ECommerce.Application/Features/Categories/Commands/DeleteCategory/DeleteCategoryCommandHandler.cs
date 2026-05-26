using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Interfaces;
using MediatR;

namespace ECommerce.Application.Features.Categories.Commands.DeleteCategory;

public class DeleteCategoryCommandHandler : IRequestHandler<DeleteCategoryCommand>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cacheService;

    public DeleteCategoryCommandHandler(IUnitOfWork unitOfWork, ICacheService cacheService)
    {
        _unitOfWork = unitOfWork;
        _cacheService = cacheService;
    }

    public async Task Handle(DeleteCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await _unitOfWork.Category.GetCategoryWithSubCategoriesAsync(request.Id);
        if (category == null)
            throw new NotFoundException(nameof(Category), request.Id);

        if (category.SubCategories.Any())
            throw new InvalidOperationException(
                $"'{category.Name}' kategorisi silinemez — once {category.SubCategories.Count} alt kategorisini silin.");

        if (await _unitOfWork.Category.HasProductsAsync(request.Id))
            throw new InvalidOperationException(
                $"'{category.Name}' kategorisi silinemez — kategoriye ait aktif urunler var.");

        _unitOfWork.Category.Delete(category);
        await _unitOfWork.SaveChangesAsync();

        // Tüm kategori cache'lerini temizle
        await _cacheService.RemoveByPrefixAsync("categories:", cancellationToken);
    }
}
