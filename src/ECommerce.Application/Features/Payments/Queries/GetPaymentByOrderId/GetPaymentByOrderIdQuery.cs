using ECommerce.Application.Features.Payments.DTOs;
using MediatR;

namespace ECommerce.Application.Features.Payments.Queries.GetPaymentByOrderId
{
    public record GetPaymentByOrderIdQuery(Guid OrderId, Guid UserId, bool IsAdmin = false) : IRequest<PaymentDTO>;
}
