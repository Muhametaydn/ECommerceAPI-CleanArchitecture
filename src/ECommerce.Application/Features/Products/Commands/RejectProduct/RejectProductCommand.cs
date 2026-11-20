using MediatR;

namespace ECommerce.Application.Features.Products.Commands.RejectProduct
{
    public record RejectProductCommand(Guid ProductId) : IRequest;
}
