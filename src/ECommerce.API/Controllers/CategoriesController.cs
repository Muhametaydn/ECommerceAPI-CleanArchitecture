using ECommerce.Application.Features.Categories.Commands.CreateCategory;
using ECommerce.Application.Features.Categories.Commands.DeleteCategory;
using ECommerce.Application.Features.Categories.Commands.UpdateCategory;
using ECommerce.Application.Features.Categories.DTOs;
using ECommerce.Application.Features.Categories.Queries.GetBreadcrumb;
using ECommerce.Application.Features.Categories.Queries.GetCategoryById;
using ECommerce.Application.Features.Categories.Queries.GetCategoryBySlug;
using ECommerce.Application.Features.Categories.Queries.GetCategoryTree;
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
    public class CategoriesController : ControllerBase
    {
        private readonly IMediator _mediator;

        public CategoriesController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Tum kategori agacini getirir (3 seviye derinlik)
        /// </summary>
        /// <remarks>
        /// Root kategoriler ve alt kategorileriyle birlikte hiyerarsik agac yapisi doner.
        /// Ornek response:
        ///
        ///     [
        ///       { "name": "Elektronik", "subCategories": [
        ///           { "name": "Telefonlar", "subCategories": [
        ///               { "name": "Akilli Telefonlar" }
        ///           ]}
        ///       ]}
        ///     ]
        /// </remarks>
        [HttpGet("tree")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(List<CategoryTreeDTO>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetTree()
        {
            var result = await _mediator.Send(new GetCategoryTreeQuery());
            return Ok(result);
        }

        /// <summary>
        /// ID ile kategori detayini getirir
        /// </summary>
        [HttpGet("{id:guid}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(CategoryDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _mediator.Send(new GetCategoryByIdQuery(id));
            return Ok(result);
        }

        /// <summary>
        /// Slug ile kategori detayini getirir (SEO-friendly URL'ler icin)
        /// </summary>
        /// <remarks>
        /// Ornek: GET /api/v1/categories/slug/akilli-telefonlar
        /// </remarks>
        [HttpGet("slug/{slug}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(CategoryDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetBySlug(string slug)
        {
            var result = await _mediator.Send(new GetCategoryBySlugQuery(slug));
            return Ok(result);
        }

        /// <summary>
        /// Kategorinin breadcrumb (kirinti yolu) bilgisini getirir
        /// </summary>
        /// <remarks>
        /// Ornek: Elektronik > Telefonlar > Akilli Telefonlar
        /// </remarks>
        [HttpGet("{id:guid}/breadcrumb")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(BreadcrumbDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetBreadcrumb(Guid id)
        {
            var result = await _mediator.Send(new GetBreadcrumbQuery(id));
            return Ok(result);
        }

        /// <summary>
        /// Yeni kategori olusturur (sadece Admin)
        /// </summary>
        [HttpPost]
        [Authorize(Policy = "AdminOnly")]
        [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] CreateCategoryCommand command)
        {
            var categoryId = await _mediator.Send(command);
            return CreatedAtAction(nameof(GetById), new { id = categoryId }, categoryId);
        }

        /// <summary>
        /// Kategoriyi gunceller (sadece Admin)
        /// </summary>
        [HttpPut("{id:guid}")]
        [Authorize(Policy = "AdminOnly")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCategoryCommand command)
        {
            command.Id = id;
            await _mediator.Send(command);
            return NoContent();
        }

        /// <summary>
        /// Kategoriyi siler (sadece Admin — alt kategorisi ve urunu olmamalı)
        /// </summary>
        [HttpDelete("{id:guid}")]
        [Authorize(Policy = "AdminOnly")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _mediator.Send(new DeleteCategoryCommand(id));
            return NoContent();
        }
    }
}
