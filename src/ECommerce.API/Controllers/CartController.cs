using System.Security.Claims;
using ECommerce.Application.Features.Cart.Commands.AddToCart;
using ECommerce.Application.Features.Cart.Commands.ClearCart;
using ECommerce.Application.Features.Cart.Commands.MergeCart;
using ECommerce.Application.Features.Cart.Commands.RemoveFromCart;
using ECommerce.Application.Features.Cart.Commands.UpdateCartItem;
using ECommerce.Application.Features.Cart.DTOs;
using ECommerce.Application.Features.Cart.Queries.GetCart;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ECommerce.API.Controllers
{
    [ApiController]
    [Route("api/v1/cart")]
    [Produces("application/json")]
    [EnableRateLimiting("api")]
    [AllowAnonymous]
    public class CartController : ControllerBase
    {
        private readonly IMediator _mediator;

        public CartController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Mevcut sepeti getirir
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(CartDTO), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetCart()
        {
            var result = await _mediator.Send(new GetCartQuery(GetCartId()));
            return Ok(result);
        }

        /// <summary>
        /// Sepete ürün ekler
        /// </summary>
        [HttpPost("items")]
        [ProducesResponseType(typeof(CartDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> AddItem([FromBody] AddToCartRequest request)
        {
            var command = new AddToCartCommand
            {
                ProductId = request.ProductId,
                Quantity = request.Quantity,
                CartId = GetCartId()
            };

            var result = await _mediator.Send(command);
            return Ok(result);
        }

        /// <summary>
        /// Sepetteki ürün miktarını günceller
        /// </summary>
        [HttpPut("items/{productId:guid}")]
        [ProducesResponseType(typeof(CartDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateItem(Guid productId, [FromBody] UpdateCartItemRequest request)
        {
            var command = new UpdateCartItemCommand
            {
                ProductId = productId,
                Quantity = request.Quantity,
                CartId = GetCartId()
            };

            var result = await _mediator.Send(command);
            return Ok(result);
        }

        /// <summary>
        /// Sepetten ürün kaldırır
        /// </summary>
        [HttpDelete("items/{productId:guid}")]
        [ProducesResponseType(typeof(CartDTO), StatusCodes.Status200OK)]
        public async Task<IActionResult> RemoveItem(Guid productId)
        {
            var command = new RemoveFromCartCommand
            {
                ProductId = productId,
                CartId = GetCartId()
            };

            var result = await _mediator.Send(command);
            return Ok(result);
        }

        /// <summary>
        /// Sepeti temizler
        /// </summary>
        [HttpDelete]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> ClearCart()
        {
            await _mediator.Send(new ClearCartCommand { CartId = GetCartId() });
            return NoContent();
        }

        /// <summary>
        /// Anonim sepeti giriş yapan kullanıcının sepetine birleştirir.
        /// Login sonrası çağrılmalıdır.
        /// </summary>
        [HttpPost("merge")]
        [Authorize(Policy = "Authenticated")]
        [ProducesResponseType(typeof(CartDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> MergeCart([FromBody] MergeCartRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var command = new MergeCartCommand
            {
                AnonymousCartId = $"cart:anon:{request.AnonymousCartId}",
                UserCartId = $"cart:{userId}"
            };

            var result = await _mediator.Send(command);
            return Ok(result);
        }

        /// <summary>
        /// Kimliği doğrulanmış kullanıcı için "cart:{userId}",
        /// anonim kullanıcı için "cart:anon:{X-Cart-Id}" döner.
        /// </summary>
        private string GetCartId()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!string.IsNullOrEmpty(userId))
                return $"cart:{userId}";

            var anonymousId = Request.Headers["X-Cart-Id"].FirstOrDefault();
            if (string.IsNullOrEmpty(anonymousId))
                anonymousId = Guid.NewGuid().ToString();

            return $"cart:anon:{anonymousId}";
        }
    }

    // Request modelleri — Controller ile aynı dosyada, minimal tutuldu
    public class AddToCartRequest
    {
        public Guid ProductId { get; set; }
        public int Quantity { get; set; }
    }

    public class UpdateCartItemRequest
    {
        public int Quantity { get; set; }
    }

    public class MergeCartRequest
    {
        public string AnonymousCartId { get; set; } = string.Empty;
    }
}
