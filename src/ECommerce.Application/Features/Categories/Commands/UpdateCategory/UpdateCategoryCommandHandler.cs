using ECommerce.Application.Common.Exceptions;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Interfaces;
using MediatR;

namespace ECommerce.Application.Features.Categories.Commands.UpdateCategory;

public class UpdateCategoryCommandHandler : IRequestHandler<UpdateCategoryCommand>
{
    private readonly IUnitOfWork _unitOfWork;

    public UpdateCategoryCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
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

        // Aktiflik durumu degistiyse
        if (request.IsActive && !category.IsActive)
            category.Activate();
        else if (!request.IsActive && category.IsActive)
            category.Deactivate();

        _unitOfWork.Category.Update(category);
        await _unitOfWork.SaveChangesAsync();
    }
}
