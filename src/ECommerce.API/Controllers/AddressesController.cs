using System.Security.Claims;
using ECommerce.Application.Features.Addresses.Commands.CreateAddress;
using ECommerce.Application.Features.Addresses.Commands.DeleteAddress;
using ECommerce.Application.Features.Addresses.Commands.UpdateAddress;
using ECommerce.Application.Features.Addresses.DTOs;
using ECommerce.Application.Features.Addresses.Queries.GetUserAddresses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ECommerce.API.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Produces("application/json")]
    [EnableRateLimiting("api")]
    [Authorize(Policy = "Authenticated")]
    public class AddressesController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AddressesController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Kullanıcının adreslerini listeler
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(IReadOnlyList<AddressDTO>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMyAddresses()
        {
            var userId = GetUserId();
            var result = await _mediator.Send(new GetUserAddressesQuery(userId));
            return Ok(result);
        }

        /// <summary>
        /// Yeni adres ekler
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(AddressDTO), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] CreateAddressRequest request)
        {
            var command = new CreateAddressCommand
            {
                UserId = GetUserId(),
                Title = request.Title,
                AddressLine = request.AddressLine,
                City = request.City,
                District = request.District,
                PostalCode = request.PostalCode,
                Country = request.Country ?? "Türkiye"
            };

            var result = await _mediator.Send(command);
            return CreatedAtAction(nameof(GetMyAddresses), result);
        }

        /// <summary>
        /// Adresi günceller
        /// </summary>
        [HttpPut("{id:guid}")]
        [ProducesResponseType(typeof(AddressDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateAddressRequest request)
        {
            var command = new UpdateAddressCommand
            {
                Id = id,
                UserId = GetUserId(),
                Title = request.Title,
                AddressLine = request.AddressLine,
                City = request.City,
                District = request.District,
                PostalCode = request.PostalCode,
                Country = request.Country ?? "Türkiye"
            };

            var result = await _mediator.Send(command);
            return Ok(result);
        }

        /// <summary>
        /// Adresi siler (aktif siparişlerde kullanılmıyorsa)
        /// </summary>
        [HttpDelete("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Delete(Guid id)
        {
            var command = new DeleteAddressCommand
            {
                Id = id,
                UserId = GetUserId()
            };

            await _mediator.Send(command);
            return NoContent();
        }

        private Guid GetUserId()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? throw new UnauthorizedAccessException("Kullanıcı bilgisi bulunamadı.");
            return Guid.Parse(userIdClaim);
        }
    }

    // ── Request modelleri ────────────────────────────────────────────────────
    public class CreateAddressRequest
    {
        public string Title { get; set; } = string.Empty;
        public string AddressLine { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string District { get; set; } = string.Empty;
        public string PostalCode { get; set; } = string.Empty;
        public string? Country { get; set; }
    }

    public class UpdateAddressRequest
    {
        public string Title { get; set; } = string.Empty;
        public string AddressLine { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string District { get; set; } = string.Empty;
        public string PostalCode { get; set; } = string.Empty;
        public string? Country { get; set; }
    }
}
