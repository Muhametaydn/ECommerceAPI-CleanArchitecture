using Asp.Versioning;
using ECommerce.Application.Features.Admin.DTOs;
using ECommerce.Application.Features.Admin.Commands.UpdateMarketplaceSettings;
using ECommerce.Application.Features.Admin.Commands.UpdateSellerRole;
using ECommerce.Application.Features.Admin.Queries.GetAdminDashboard;
using ECommerce.Application.Features.Admin.Queries.GetAdminUsers;
using ECommerce.Application.Features.Admin.Queries.GetMarketplaceSettings;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ECommerce.API.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v1/admin")]
    [Produces("application/json")]
    [EnableRateLimiting("api")]
    [Authorize(Policy = "AdminOnly")]
    public class AdminController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AdminController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("users")]
        [ProducesResponseType(typeof(IReadOnlyList<AdminUserDTO>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetUsers()
            => Ok(await _mediator.Send(new GetAdminUsersQuery()));

        [HttpPut("users/{userId:guid}/seller-role")]
        [ProducesResponseType(typeof(AdminUserDTO), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateSellerRole(
            Guid userId,
            [FromBody] UpdateSellerRoleRequest request)
            => Ok(await _mediator.Send(new UpdateSellerRoleCommand(userId, request.IsSeller)));

        [HttpGet("dashboard")]
        [ProducesResponseType(typeof(AdminDashboardStatsDTO), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetDashboard()
            => Ok(await _mediator.Send(new GetAdminDashboardQuery()));

        [HttpGet("marketplace-settings")]
        [ProducesResponseType(typeof(MarketplaceSettingsDTO), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMarketplaceSettings()
            => Ok(await _mediator.Send(new GetMarketplaceSettingsQuery()));

        [HttpPut("marketplace-settings")]
        [ProducesResponseType(typeof(MarketplaceSettingsDTO), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateMarketplaceSettings(
            [FromBody] UpdateMarketplaceSettingsCommand command)
            => Ok(await _mediator.Send(command));
    }
}
