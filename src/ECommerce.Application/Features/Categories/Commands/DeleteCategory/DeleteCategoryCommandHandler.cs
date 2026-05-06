using ECommerce.Application.Common.Exceptions;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Interfaces;
using MediatR;

namespace ECommerce.Application.Features.Categories.Commands.DeleteCategory;

public class DeleteCategoryCommandHandler : IRequestHandler<DeleteCategoryCommand>
{
    private readonly IUnitOfWork _unitOfWork;

    public DeleteCategoryCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DeleteCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await _unitOfWork.Category.GetCategoryWithSubCategoriesAsync(request.Id);
        if (category == null)
            throw new NotFoundException(nameof(Category), request.Id);

        // Alt kategorisi varsa silmeye izin verme
        if (category.SubCategories.Any())
            throw new InvalidOperationException(
                $"'{category.Name}' kategorisi silinemez — once {category.SubCategories.Count} alt kategorisini silin.");

        // Urunu varsa silmeye izin verme
        if (await _unitOfWork.Category.HasProductsAsync(request.Id))
            throw new InvalidOperationException(
                $"'{category.Name}' kategorisi silinemez — kategoriye ait aktif urunler var.");

        _unitOfWork.Category.Delete(category);
        await _unitOfWork.SaveChangesAsync();
    }
}
