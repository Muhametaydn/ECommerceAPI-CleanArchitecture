namespace ECommerce.Application.Features.Addresses.DTOs
{
    public class AddressDTO
    {
        public Guid Id { get; init; }
        public string Title { get; init; } = string.Empty;
        public string AddressLine { get; init; } = string.Empty;
        public string City { get; init; } = string.Empty;
        public string District { get; init; } = string.Empty;
        public string PostalCode { get; init; } = string.Empty;
        public string Country { get; init; } = string.Empty;
        public DateTime CreatedAt { get; init; }
    }
}
