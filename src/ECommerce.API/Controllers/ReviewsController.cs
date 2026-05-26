using Asp.Versioning;
using System.Security.Claims;
using ECommerce.Application.Features.Reviews.Commands.CreateReview;
using ECommerce.Application.Features.Reviews.Commands.DeleteReview;
using ECommerce.Application.Features.Reviews.Commands.UpdateReview;
using ECommerce.Application.Features.Reviews.DTOs;
using ECommerce.Application.Features.Reviews.Queries.GetProductReviews;
using ECommerce.Application.Features.Reviews.Queries.GetUserReviews;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ECommerce.API.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v1")]
    [Produces("application/json")]
    [EnableRateLimiting("api")]
    public class ReviewsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public ReviewsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Ürünün değerlendirmelerini getirir (ortalama puan dahil)
        /// </summary>
        [HttpGet("products/{productId:guid}/reviews")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ProductReviewSummaryDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetProductReviews(Guid productId)
        {
            var result = await _mediator.Send(new GetProductReviewsQuery(productId));
            return Ok(result);
        }

        /// <summary>
        /// Kullanıcının kendi değerlendirmelerini listeler
        /// </summary>
        [HttpGet("reviews/me")]
        [Authorize(Policy = "Authenticated")]
        [ProducesResponseType(typeof(IReadOnlyList<ReviewDTO>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMyReviews()
        {
            var userId = GetUserId();
            var result = await _mediator.Send(new GetUserReviewsQuery(userId));
            return Ok(result);
        }

        /// <summary>
        /// Ürüne değerlendirme ekler.
        /// Sadece ürünü satın almış (teslim edilmiş) kullanıcılar değerlendirebilir.
        /// Her kullanıcı bir ürünü yalnızca bir kez değerlendirebilir.
        /// </summary>
        [HttpPost("products/{productId:guid}/reviews")]
        [Authorize(Policy = "Authenticated")]
        [ProducesResponseType(typeof(ReviewDTO), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create(Guid productId, [FromBody] CreateReviewRequest request)
        {
            var command = new CreateReviewCommand
            {
                UserId = GetUserId(),
                ProductId = productId,
                Title = request.Title,
                Comment = request.Comment,
                Rating = request.Rating
            };

            var result = await _mediator.Send(command);
            return CreatedAtAction(nameof(GetProductReviews), new { productId }, result);
        }

        /// <summary>
        /// Değerlendirmeyi günceller (sadece kendi değerlendirmesi)
        /// </summary>
        [HttpPut("reviews/{id:guid}")]
        [Authorize(Policy = "Authenticated")]
        [ProducesResponseType(typeof(ReviewDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateReviewRequest request)
        {
            var command = new UpdateReviewCommand
            {
                Id = id,
                UserId = GetUserId(),
                Title = request.Title,
                Comment = request.Comment,
                Rating = request.Rating
            };

            var result = await _mediator.Send(command);
            return Ok(result);
        }

        /// <summary>
        /// Değerlendirmeyi siler.
        /// Kullanıcı kendi değerlendirmesini, Admin herhangi birini silebilir.
        /// </summary>
        [HttpDelete("reviews/{id:guid}")]
        [Authorize(Policy = "Authenticated")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(Guid id)
        {
            var command = new DeleteReviewCommand
            {
                Id = id,
                UserId = GetUserId(),
                IsAdmin = User.IsInRole("Admin")
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
    public class CreateReviewRequest
    {
        public string Title { get; set; } = string.Empty;
        public string Comment { get; set; } = string.Empty;
        public int Rating { get; set; }
    }

    public class UpdateReviewRequest
    {
        public string Title { get; set; } = string.Empty;
        public string Comment { get; set; } = string.Empty;
        public int Rating { get; set; }
    }
}
