using Asp.Versioning;
using System.Text.Json.Serialization;
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
    [ApiVersion("1.0")]
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
        public async Task<IActionResult> GetCart([FromHeader(Name = "X-Cart-Id")] string? anonymousId = null)
        {
            var result = await _mediator.Send(new GetCartQuery(GetCartId(anonymousId)));
            return Ok(result);
        }

        /// <summary>
        /// Sepete ürün ekler
        /// </summary>
        [HttpPost("items")]
        [ProducesResponseType(typeof(CartDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> AddItem([FromBody] AddToCartRequest request, [FromHeader(Name = "X-Cart-Id")] string? anonymousId = null)
        {
            var command = new AddToCartCommand
            {
                ProductId = request.ProductId,
                Quantity = request.Quantity,
                CartId = GetCartId(anonymousId)
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
        public async Task<IActionResult> UpdateItem(Guid productId, [FromBody] UpdateCartItemRequest request, [FromHeader(Name = "X-Cart-Id")] string? anonymousId = null)
        {
            var command = new UpdateCartItemCommand
            {
                ProductId = productId,
                Quantity = request.Quantity,
                CartId = GetCartId(anonymousId)
            };

            var result = await _mediator.Send(command);
            return Ok(result);
        }

        /// <summary>
        /// Sepetten ürün kaldırır
        /// </summary>
        [HttpDelete("items/{productId:guid}")]
        [ProducesResponseType(typeof(CartDTO), StatusCodes.Status200OK)]
        public async Task<IActionResult> RemoveItem(Guid productId, [FromHeader(Name = "X-Cart-Id")] string? anonymousId = null)
        {
            var command = new RemoveFromCartCommand
            {
                ProductId = productId,
                CartId = GetCartId(anonymousId)
            };

            var result = await _mediator.Send(command);
            return Ok(result);
        }

        /// <summary>
        /// Sepeti temizler
        /// </summary>
        [HttpDelete]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> ClearCart([FromHeader(Name = "X-Cart-Id")] string? anonymousId = null)
        {
            await _mediator.Send(new ClearCartCommand { CartId = GetCartId(anonymousId) });
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
                AnonymousCartId = $"cart:anon:{ValidateAnonymousId(request.AnonymousCartId)}",
                UserCartId = $"cart:{userId}"
            };

            var result = await _mediator.Send(command);
            return Ok(result);
        }

        /// <summary>
        /// Kimliği doğrulanmış kullanıcı için "cart:{userId}",
        /// anonim kullanıcı için "cart:anon:{X-Cart-Id}" döner.
        /// </summary>
        private string GetCartId(string? anonymousId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!string.IsNullOrEmpty(userId))
                return $"cart:{userId}";

            return $"cart:anon:{(string.IsNullOrEmpty(anonymousId) ? Guid.NewGuid().ToString() : ValidateAnonymousId(anonymousId))}";
        }

        private static string ValidateAnonymousId(string? anonymousId)
        {
            if (anonymousId is null || anonymousId.Length != 36 ||
                !Guid.TryParseExact(anonymousId, "D", out var id) || id == Guid.Empty)
                throw new FluentValidation.ValidationException(new[]
                {
                    new FluentValidation.Results.ValidationFailure("AnonymousCartId", "Anonymous cart ID must be a non-empty UUID in D format.")
                });

            return anonymousId;
        }
    }

    // Request modelleri — Controller ile aynı dosyada, minimal tutuldu
    public class AddToCartRequest
    {
        [JsonRequired]
        public Guid ProductId { get; set; }
        [JsonRequired]
        public int Quantity { get; set; }
    }

    public class UpdateCartItemRequest
    {
        [JsonRequired]
        public int Quantity { get; set; }
    }

    public class MergeCartRequest
    {
        public string AnonymousCartId { get; set; } = string.Empty;
    }
}
