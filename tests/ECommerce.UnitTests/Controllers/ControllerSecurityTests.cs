using System.Net;
using System.Security.Claims;
using ECommerce.API.Controllers;
using ECommerce.Application.Contracts.Identity;
using ECommerce.Application.Features.Auth.DTOs;
using ECommerce.Application.Features.Cart.DTOs;
using ECommerce.Application.Features.Cart.Queries.GetCart;
using FluentAssertions;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace ECommerce.UnitTests.Controllers;

public sealed class ControllerSecurityTests
{
    [Theory]
    [InlineData("198.51.100.25", "198.51.100.25")]
    [InlineData(null, "unknown")]
    public async Task Login_IgnoresUntrustedForwardedFor(string? remoteIp, string expectedIp)
    {
        var auth = new Mock<IAuthService>();
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = remoteIp is null ? null : IPAddress.Parse(remoteIp);
        context.Request.Headers["X-Forwarded-For"] = "203.0.113.10";
        var controller = new AuthController(auth.Object, Mock.Of<IMediator>())
        {
            ControllerContext = new ControllerContext { HttpContext = context }
        };
        var request = new LoginRequest();

        await controller.Login(request);

        auth.Verify(value => value.LoginAsync(request, expectedIp), Times.Once);
    }

    [Theory]
    [InlineData("get")]
    [InlineData("add")]
    [InlineData("update")]
    [InlineData("remove")]
    [InlineData("clear")]
    [InlineData("merge")]
    public async Task Cart_RejectsMalformedIdBeforeDispatch(string operation)
    {
        var mediator = new Mock<IMediator>(MockBehavior.Strict);
        var controller = CreateCartController(mediator.Object);
        const string invalidId = "another:cart:*";

        Func<Task> action = () => operation switch
        {
            "get" => controller.GetCart(invalidId),
            "add" => controller.AddItem(new AddToCartRequest(), invalidId),
            "update" => controller.UpdateItem(Guid.NewGuid(), new UpdateCartItemRequest(), invalidId),
            "remove" => controller.RemoveItem(Guid.NewGuid(), invalidId),
            "clear" => controller.ClearCart(invalidId),
            _ => controller.MergeCart(new MergeCartRequest { AnonymousCartId = invalidId })
        };

        await action.Should().ThrowAsync<ValidationException>();
        mediator.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    [InlineData("a1111111111111111111111111111111")]
    [InlineData("a1111111-1111-1111-1111-111111111111,another")]
    public async Task Cart_RejectsEmptyOrNonCanonicalUuid(string id)
    {
        var controller = CreateCartController(Mock.Of<IMediator>());
        Func<Task> action = () => controller.GetCart(id);
        await action.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Cart_UsesBoundAnonymousId()
    {
        var mediator = CreateCartMediator();
        var controller = CreateCartController(mediator.Object);
        var id = Guid.NewGuid().ToString();

        await controller.GetCart(id);

        mediator.Verify(value => value.Send(It.Is<GetCartQuery>(q => q.CartId == $"cart:anon:{id}"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Cart_MissingHeaderCreatesUuid()
    {
        var mediator = CreateCartMediator();
        var controller = CreateCartController(mediator.Object);
        GetCartQuery? dispatched = null;
        mediator.Setup(value => value.Send(It.IsAny<GetCartQuery>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<CartDTO>, CancellationToken>((query, _) => dispatched = (GetCartQuery)query)
            .ReturnsAsync(new CartDTO());

        await controller.GetCart();

        dispatched.Should().NotBeNull();
        dispatched!.CartId.Should().StartWith("cart:anon:");
        Guid.TryParseExact(dispatched.CartId[10..], "D", out var id).Should().BeTrue();
        id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task Cart_AuthenticatedUserCannotChooseAnotherCartViaHeader()
    {
        var mediator = CreateCartMediator();
        var controller = CreateCartController(mediator.Object);
        var userId = Guid.NewGuid().ToString();
        controller.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.NameIdentifier, userId) }, "test"));

        await controller.GetCart(Guid.NewGuid().ToString());

        mediator.Verify(value => value.Send(It.Is<GetCartQuery>(q => q.CartId == $"cart:{userId}"), It.IsAny<CancellationToken>()), Times.Once);
    }

    private static Mock<IMediator> CreateCartMediator()
    {
        var mediator = new Mock<IMediator>();
        mediator.Setup(value => value.Send(It.IsAny<GetCartQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CartDTO());
        return mediator;
    }

    private static CartController CreateCartController(IMediator mediator) => new(mediator)
    {
        ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
    };
}
