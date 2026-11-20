using MediatR;

namespace ECommerce.Application.Features.Products.Commands.ApproveProduct
{
    public record ApproveProductCommand(Guid ProductId) : IRequest;
}
