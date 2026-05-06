using ECommerce.Application.Features.Addresses.DTOs;
using MediatR;

namespace ECommerce.Application.Features.Addresses.Queries.GetUserAddresses
{
    public record GetUserAddressesQuery(Guid UserId) : IRequest<IReadOnlyList<AddressDTO>>;
}
