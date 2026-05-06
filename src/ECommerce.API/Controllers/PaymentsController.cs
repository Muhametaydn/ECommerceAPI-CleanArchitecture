using System.Security.Claims;
using ECommerce.Application.Features.Payments.Commands.RefundPayment;
using ECommerce.Application.Features.Payments.DTOs;
using ECommerce.Application.Features.Payments.Queries.GetPaymentByOrderId;
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
    public class PaymentsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public PaymentsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Sipariş ID ile ödeme bilgisini getirir.
        /// Müşteri sadece kendi siparişinin ödemesini görebilir.
        /// </summary>
        [HttpGet("order/{orderId:guid}")]
        [ProducesResponseType(typeof(PaymentDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetByOrderId(Guid orderId)
        {
            var userId = GetUserId();
            var isAdmin = User.IsInRole("Admin");
            var result = await _mediator.Send(new GetPaymentByOrderIdQuery(orderId, userId, isAdmin));
            return Ok(result);
        }

        /// <summary>
        /// Ödeme iadesi yapar (Admin).
        /// Sipariş durumu Refunded olur ve stoklar geri eklenir.
        /// </summary>
        [HttpPost("order/{orderId:guid}/refund")]
        [Authorize(Policy = "AdminOnly")]
        [ProducesResponseType(typeof(PaymentDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Refund(Guid orderId)
        {
            var result = await _mediator.Send(new RefundPaymentCommand { OrderId = orderId });
            return Ok(result);
        }

        private Guid GetUserId()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? throw new UnauthorizedAccessException("Kullanıcı bilgisi bulunamadı.");
            return Guid.Parse(userIdClaim);
        }
    }
}
