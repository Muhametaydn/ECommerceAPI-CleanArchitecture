using Asp.Versioning;
using System.Security.Claims;
using ECommerce.Application.Common.Models;
using ECommerce.Application.Features.Products.Commands.CreateProduct;
using ECommerce.Application.Features.Products.DTOs;
using ECommerce.Application.Features.Products.Queries.GetAllProducts;
using ECommerce.Application.Features.Products.Queries.GetProductById;
using ECommerce.Application.Features.Products.Queries.SearchProducts;
using ECommerce.Application.Features.Products.Commands.UpdateProduct;
using ECommerce.Application.Features.Products.Commands.DeleteProduct;
using ECommerce.Application.Features.Products.Commands.ApproveProduct;
using ECommerce.Application.Features.Products.Commands.RejectProduct;
using ECommerce.Application.Features.Products.Queries.GetAdminProducts;
using ECommerce.Application.Features.Products.Queries.GetSellerProducts;
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
    public class ProductsController : ControllerBase
    {

        private readonly IMediator _mediator;

        public ProductsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Urunleri listeler (filtreleme, arama, sayfalama destekli)
        /// </summary>
        /// <remarks>
        /// Ornek istekler:
        ///
        ///     GET /api/v1/products?searchTerm=laptop&amp;minPrice=1000&amp;maxPrice=5000
        ///     GET /api/v1/products?categoryId=xxx&amp;sortBy=price&amp;sortDescending=true
        ///     GET /api/v1/products?inStock=true&amp;pageSize=20&amp;pageNumber=2
        ///     GET /api/v1/products?cursor=xxx&amp;pageSize=10  (cursor-based pagination)
        ///
        /// Desteklenen sortBy degerleri: price, name, stock, date
        /// </remarks>
        [HttpGet]
        [AllowAnonymous]
        [ProducesResponseType(typeof(PaginatedResult<ProductDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetAll([FromQuery] GetAllProductsQuery query)
        {
            var result = await _mediator.Send(query);
            return Ok(result);
        }

        /// <summary>
        /// Yeni urun olusturur (Admin veya Seller)
        /// </summary>
        [HttpPost]
        [Authorize(Policy = "SellerOrAdmin")]
        [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] CreateProductCommand command) {
            command.RequesterId = GetUserId();
            command.IsAdmin = User.IsInRole("Admin");
            var productId = await _mediator.Send(command);
            return CreatedAtAction(nameof(GetById), new { id = productId }, productId);
        }

        /// <summary>
        /// ID ile urun detayini getirir
        /// </summary>
        [HttpGet("{id:guid}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ProductDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _mediator.Send(new GetProductByIdQuery(id));
            return Ok(result);
        }

        /// <summary>
        /// Urunu gunceller (Admin veya Seller)
        /// </summary>
        [HttpPut("{id:guid}")]
        [Authorize(Policy = "SellerOrAdmin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProductCommand command)
        {
            command.Id = id;
            command.RequesterId = GetUserId();
            command.IsAdmin = User.IsInRole("Admin");
            await _mediator.Send(command);
            return NoContent();
        }

        [HttpGet("admin")]
        [Authorize(Policy = "AdminOnly")]
        [ProducesResponseType(typeof(IReadOnlyList<ProductDTO>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAdminProducts()
            => Ok(await _mediator.Send(new GetAdminProductsQuery()));

        [HttpGet("mine")]
        [Authorize(Policy = "SellerOrAdmin")]
        [ProducesResponseType(typeof(IReadOnlyList<ProductDTO>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMyProducts()
            => Ok(await _mediator.Send(new GetSellerProductsQuery(GetUserId())));

        [HttpPost("{id:guid}/approve")]
        [Authorize(Policy = "AdminOnly")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> Approve(Guid id)
        {
            await _mediator.Send(new ApproveProductCommand(id));
            return NoContent();
        }

        [HttpPost("{id:guid}/reject")]
        [Authorize(Policy = "AdminOnly")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> Reject(Guid id)
        {
            await _mediator.Send(new RejectProductCommand(id));
            return NoContent();
        }

        /// <summary>
        /// Urunu siler (sadece Admin)
        /// </summary>
        [HttpDelete("{id:guid}")]
        [Authorize(Policy = "AdminOnly")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _mediator.Send(new DeleteProductCommand(id));
            return NoContent();
        }

        /// <summary>
        /// Elasticsearch ile full-text urun arama
        /// </summary>
        /// <remarks>
        /// DB sorgusu yerine Elasticsearch kullanır. Büyük kataloglarda çok daha hızlı.
        ///
        /// Ornek istekler:
        ///
        ///     GET /api/v1/products/search?query=laptop
        ///     GET /api/v1/products/search?query=iphone&amp;minPrice=10000&amp;categoryId=xxx
        ///     GET /api/v1/products/search?inStock=true&amp;sortBy=price&amp;sortDescending=false
        ///
        /// Fuzzy search destekler (yazım hatası toleransı: "laptpo" → "laptop")
        /// </remarks>
        [HttpGet("search")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ProductSearchResult), StatusCodes.Status200OK)]
        public async Task<IActionResult> Search(
            [FromQuery] string? query,
            [FromQuery] Guid? categoryId,
            [FromQuery] decimal? minPrice,
            [FromQuery] decimal? maxPrice,
            [FromQuery] bool? inStock,
            [FromQuery] string? sortBy,
            [FromQuery] bool sortDescending = false,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            var result = await _mediator.Send(new SearchProductsQuery(
                query, categoryId, minPrice, maxPrice, inStock,
                sortBy, sortDescending, pageNumber, pageSize));
            return Ok(result);
        }

        private Guid GetUserId()
        {
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? throw new UnauthorizedAccessException("Kullanıcı bilgisi bulunamadı.");
            return Guid.Parse(value);
        }
    }
}
