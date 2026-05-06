using ECommerce.Domain.Entities;
using ECommerce.Domain.Interfaces;
using MediatR;

namespace ECommerce.Application.Features.Categories.Commands.CreateCategory;

public class CreateCategoryCommandHandler : IRequestHandler<CreateCategoryCommand, Guid>
{
    private readonly IUnitOfWork _unitOfWork;

    public CreateCategoryCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        // Slug olustur ve benzersizligini kontrol et
        var slug = Category.GenerateSlug(request.Name);
        if (await _unitOfWork.Category.SlugExistsAsync(slug))
        {
            // Slug zaten varsa sonuna numara ekle
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

            category.SetParent(parent); // Derinlik kontrolu burada yapilir
        }

        await _unitOfWork.Category.AddAsync(category);
        await _unitOfWork.SaveChangesAsync();

        return category.Id;
    }
}
