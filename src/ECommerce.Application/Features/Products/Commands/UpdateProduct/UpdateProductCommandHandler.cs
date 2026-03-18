using MediatR;
using ECommerce.Domain.Interfaces;
using ECommerce.Application.Common.Exceptions;

namespace ECommerce.Application.Features.Products.Commands.UpdateProduct;

public class UpdateProductCommandHandler : IRequestHandler<UpdateProductCommand, bool>
{
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateProductCommandHandler(IProductRepository productRepository, IUnitOfWork unitOfWork)
    {
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetByIdAsync(request.Id);

        if (product is null)
            throw new NotFoundException("Product", request.Id);

        product.Name = request.Name;
        product.Description = request.Description;
        product.UpdatePrice(request.Price);
        product.StockQuantity = request.StockQuantity;
        product.SKU = request.SKU;
        product.IsActive = request.IsActive;
        product.CategoryId = request.CategoryId;

        _productRepository.Update(product);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }
}