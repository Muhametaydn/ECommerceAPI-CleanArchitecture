using Asp.Versioning;
using ECommerce.Application.Features.Coupons.Commands.CreateCoupon;
using ECommerce.Application.Features.Coupons.Commands.DeleteCoupon;
using ECommerce.Application.Features.Coupons.DTOs;
using ECommerce.Application.Features.Coupons.Queries.GetAllCoupons;
using ECommerce.Application.Features.Coupons.Queries.ValidateCoupon;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ECommerce.API.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v1/[controller]")]
    [Produces("application/json")]
    [EnableRateLimiting("api")]
    public class CouponsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public CouponsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Tüm kuponları listeler (Admin)
        /// </summary>
        [HttpGet]
        [Authorize(Policy = "AdminOnly")]
        [ProducesResponseType(typeof(IReadOnlyList<CouponDTO>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll([FromQuery] bool includeInactive = false)
        {
            var result = await _mediator.Send(new GetAllCouponsQuery(includeInactive));
            return Ok(result);
        }

        /// <summary>
        /// Yeni kupon oluşturur (Admin)
        /// </summary>
        [HttpPost]
        [Authorize(Policy = "AdminOnly")]
        [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] CreateCouponCommand command)
        {
            var couponId = await _mediator.Send(command);
            return Created($"/api/v1/coupons/{couponId}", couponId);
        }

        /// <summary>
        /// Kuponu deaktif eder (Admin)
        /// </summary>
        [HttpDelete("{id:guid}")]
        [Authorize(Policy = "AdminOnly")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _mediator.Send(new DeleteCouponCommand(id));
            return NoContent();
        }

        /// <summary>
        /// Kupon kodunu doğrular ve indirim tutarını hesaplar.
        /// Checkout sırasında müşteri tarafından kullanılır.
        /// </summary>
        /// <remarks>
        /// Örnek istek:
        ///
        ///     POST /api/v1/coupons/validate
        ///     { "code": "YENI50", "orderTotal": 500.00 }
        ///
        /// </remarks>
        [HttpPost("validate")]
        [Authorize(Policy = "Authenticated")]
        [ProducesResponseType(typeof(CouponValidationDTO), StatusCodes.Status200OK)]
        public async Task<IActionResult> Validate([FromBody] ValidateCouponQuery query)
        {
            var result = await _mediator.Send(query);
            return Ok(result);
        }
    }
}
