using Asp.Versioning;
using System.Security.Claims;
using ECommerce.Application.Features.Orders.Commands.CancelOrder;
using ECommerce.Application.Features.Orders.Commands.CreateOrder;
using ECommerce.Application.Features.Orders.Commands.UpdateOrderStatus;
using ECommerce.Application.Features.Orders.Commands.UpdateOrderItemStatus;
using ECommerce.Application.Features.Orders.DTOs;
using ECommerce.Application.Features.Orders.Queries.GetAllOrders;
using ECommerce.Application.Features.Orders.Queries.GetOrderById;
using ECommerce.Application.Features.Orders.Queries.GetSellerOrders;
using ECommerce.Application.Features.Orders.Queries.GetUserOrders;
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
    [Authorize(Policy = "Authenticated")]
    public class OrdersController : ControllerBase
    {
        private readonly IMediator _mediator;

        public OrdersController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Sepetten yeni sipariş oluşturur.
        /// Stok kontrolü yapılır, ödeme kaydı oluşturulur ve sepet temizlenir.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(OrderDetailDTO), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
        {
            var userId = GetUserId();
            var command = new CreateOrderCommand
            {
                CheckoutRequestId = request.CheckoutRequestId,
                UserId = userId,
                CartId = $"cart:{userId}",
                ShippingAddressId = request.ShippingAddressId,
                PaymentMethod = request.PaymentMethod,
                Note = request.Note,
                CouponCode = request.CouponCode,
                // UseForwardedHeaders sayesinde yalnızca güvenilir proxy'den gelen X-Forwarded-For kullanılır.
                ClientIpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                UserAgent = Request.Headers.UserAgent.ToString()
            };

            var result = await _mediator.Send(command);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        /// <summary>
        /// Giriş yapan kullanıcının siparişlerini listeler
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(IReadOnlyList<OrderDTO>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMyOrders()
        {
            var userId = GetUserId();
            var result = await _mediator.Send(new GetUserOrdersQuery(userId));
            return Ok(result);
        }

        /// <summary>
        /// Yöneticinin mağazadaki bütün siparişleri listelemesini sağlar.
        /// </summary>
        [HttpGet("admin")]
        [Authorize(Policy = "AdminOnly")]
        [ProducesResponseType(typeof(IReadOnlyList<OrderDTO>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllOrders()
        {
            var result = await _mediator.Send(new GetAllOrdersQuery());
            return Ok(result);
        }

        /// <summary>
        /// Satıcının yalnızca kendi ürünlerini içeren sipariş kalemlerini listeler.
        /// </summary>
        [HttpGet("seller")]
        [Authorize(Policy = "SellerOrAdmin")]
        [ProducesResponseType(typeof(IReadOnlyList<SellerOrderDTO>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetSellerOrders()
            => Ok(await _mediator.Send(new GetSellerOrdersQuery(GetUserId())));

        /// <summary>
        /// Sipariş detayını getirir.
        /// Müşteri sadece kendi siparişini, Admin tüm siparişleri görebilir.
        /// </summary>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(OrderDetailDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(Guid id)
        {
            var userId = GetUserId();
            var isAdmin = User.IsInRole("Admin");
            var result = await _mediator.Send(new GetOrderByIdQuery(id, userId, isAdmin));
            return Ok(result);
        }

        /// <summary>
        /// Sipariş durumunu günceller (Admin veya Seller).
        /// Desteklenen aksiyonlar: confirm, ship, deliver
        /// </summary>
        /// <remarks>
        /// Örnek istek:
        ///
        ///     PATCH /api/v1/orders/{id}/status
        ///     { "action": "confirm" }
        ///
        /// Durum akışı: Pending → Confirmed → Shipped → Delivered
        /// </remarks>
        [HttpPatch("{id:guid}/status")]
        [Authorize(Policy = "SellerOrAdmin")]
        [ProducesResponseType(typeof(OrderDetailDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateOrderStatusRequest request)
        {
            var command = new UpdateOrderStatusCommand
            {
                OrderId = id,
                Action = request.Action,
                RequesterId = GetUserId(),
                IsAdmin = User.IsInRole("Admin")
            };

            var result = await _mediator.Send(command);
            return Ok(result);
        }

        /// <summary>
        /// Satıcının yalnızca kendisine ait sipariş kalemini ilerletmesini sağlar.
        /// Durum akışı: Pending → Preparing → Shipped → Delivered
        /// </summary>
        [HttpPatch("{id:guid}/items/{itemId:guid}/status")]
        [Authorize(Policy = "SellerOrAdmin")]
        [ProducesResponseType(typeof(OrderItemDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateItemStatus(
            Guid id,
            Guid itemId,
            [FromBody] UpdateOrderItemStatusRequest request)
        {
            var result = await _mediator.Send(new UpdateOrderItemStatusCommand
            {
                OrderId = id,
                OrderItemId = itemId,
                Action = request.Action,
                RequesterId = GetUserId(),
                IsAdmin = User.IsInRole("Admin")
            });

            return Ok(result);
        }

        /// <summary>
        /// Müşterinin kendi siparişini iptal etmesi.
        /// Sadece Pending/Confirmed durumundaki siparişler iptal edilebilir.
        /// İptal edilen siparişlerin stokları geri eklenir.
        /// </summary>
        [HttpPost("{id:guid}/cancel")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CancelOrder(Guid id)
        {
            var userId = GetUserId();
            var command = new CancelOrderCommand
            {
                OrderId = id,
                UserId = userId
            };

            await _mediator.Send(command);
            return NoContent();
        }

        // ── Yardımcı ────────────────────────────────────────────────────────
        private Guid GetUserId()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? throw new UnauthorizedAccessException("Kullanıcı bilgisi bulunamadı.");
            return Guid.Parse(userIdClaim);
        }
    }

    // ── Request modelleri ────────────────────────────────────────────────────
    public class UpdateOrderStatusRequest
    {
        public string Action { get; set; } = string.Empty;
    }

    public class UpdateOrderItemStatusRequest
    {
        public string Action { get; set; } = string.Empty;
    }
}
