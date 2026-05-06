using ECommerce.Application.Features.Addresses.DTOs;
using MediatR;

namespace ECommerce.Application.Features.Addresses.Commands.CreateAddress
{
    public class CreateAddressCommand : IRequest<AddressDTO>
    {
        public Guid UserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string AddressLine { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string District { get; set; } = string.Empty;
        public string PostalCode { get; set; } = string.Empty;
        public string Country { get; set; } = "Türkiye";
    }
}
