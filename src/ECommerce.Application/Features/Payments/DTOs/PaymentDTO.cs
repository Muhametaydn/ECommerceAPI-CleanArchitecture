using ECommerce.Domain.Enums;

namespace ECommerce.Application.Features.Payments.DTOs
{
    public class PaymentDTO
    {
        public Guid Id { get; init; }
        public Guid OrderId { get; init; }
        public string OrderNumber { get; init; } = string.Empty;
        public decimal Amount { get; init; }
        public PaymentMethod Method { get; init; }
        public string MethodText => Method.ToString();
        public PaymentStatus Status { get; init; }
        public string StatusText => Status.ToString();
        public string? TransactionId { get; init; }
        public DateTime CreatedAt { get; init; }
        public DateTime? UpdatedAt { get; init; }
    }
}
