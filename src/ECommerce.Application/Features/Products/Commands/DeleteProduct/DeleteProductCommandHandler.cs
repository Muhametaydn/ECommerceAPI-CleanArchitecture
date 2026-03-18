using MediatR;
using ECommerce.Domain.Interfaces;
using ECommerce.Application.Common.Exceptions;

namespace ECommerce.Application.Features.Products.Commands.DeleteProduct;

public class DeleteProductCommandHandler : IRequestHandler<DeleteProductCommand, bool>
{
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteProductCommandHandler(IProductRepository productRepository, IUnitOfWork unitOfWork)
    {
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetByIdAsync(request.Id);

        if (product is null)
            throw new NotFoundException("Product", request.Id);

        _productRepository.Delete(product);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }
}