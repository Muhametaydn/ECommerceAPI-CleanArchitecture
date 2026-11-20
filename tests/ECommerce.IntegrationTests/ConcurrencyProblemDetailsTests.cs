using System.Text.Json;
using ECommerce.API.Middlewares;
using ECommerce.Application.Common.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace ECommerce.IntegrationTests;

public class ConcurrencyProblemDetailsTests
{
    [Fact]
    public async Task Conflict_is_409_problem_details_without_database_exception_details()
    {
        var middleware = new GlobalExceptionMiddleware(_ => throw new ConflictException(new Exception("private database details")), NullLogger<GlobalExceptionMiddleware>.Instance);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        await middleware.InvokeAsync(context);
        Assert.Equal(409, context.Response.StatusCode);
        Assert.Equal("application/problem+json", context.Response.ContentType);
        context.Response.Body.Position = 0;
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        Assert.DoesNotContain("private database details", body);
        using var problem = JsonDocument.Parse(body);
        Assert.Equal(409, problem.RootElement.GetProperty("status").GetInt32());
    }
}