using System.Text.Json;
using ECommerce.API.Middlewares;
using ECommerce.Application.Common.Exceptions;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace ECommerce.UnitTests.Middlewares;

public class GlobalExceptionMiddlewareTests
{
    [Theory]
    [MemberData(nameof(MappedExceptions))]
    public async Task Maps_application_exceptions_to_problem_details(Exception exception, int status, string expectedDetail)
    {
        var (statusCode, body) = await Invoke(() => throw exception);

        statusCode.Should().Be(status);
        body.GetProperty("status").GetInt32().Should().Be(status);
        body.GetProperty("detail").GetString().Should().Be(expectedDetail);
    }

    public static TheoryData<Exception, int, string> MappedExceptions() => new()
    {
        { new NotFoundException("Ürün", "42"), 404, "Ürün bulunamadı. ID: 42" },
        { new ConflictException("çakışma"), 409, "çakışma" },
        { new ForbiddenException("yasak"), 403, "yasak" },
        { new UnauthorizedAccessException("giriş gerekli"), 401, "giriş gerekli" },
        { new InvalidOperationException("Yetersiz stok."), 400, "Yetersiz stok." },
    };

    [Fact]
    public async Task Validation_errors_are_grouped_by_property()
    {
        var failures = new[]
        {
            new ValidationFailure("Email", "boş olamaz"),
            new ValidationFailure("Email", "geçersiz"),
            new ValidationFailure("Password", "kısa")
        };

        var (status, body) = await Invoke(() => throw new ValidationException(failures));

        status.Should().Be(400);
        var errors = body.GetProperty("errors");
        errors.GetProperty("Email").GetArrayLength().Should().Be(2);
        errors.GetProperty("Password")[0].GetString().Should().Be("kısa");
    }

    [Fact]
    public async Task Library_invalid_operation_is_500_and_does_not_leak_internal_message()
    {
        // System.Linq'in fırlattığı InvalidOperationException: iç ayrıntı istemciye gitmemeli.
        var (status, body) = await Invoke(() =>
        {
            _ = Enumerable.Empty<int>().First();
            return Task.CompletedTask;
        });

        status.Should().Be(500);
        body.GetProperty("detail").GetString().Should().Be("Beklenmeyen bir hata oluştu.");
        body.GetRawText().Should().NotContain("Sequence contains no elements");
    }

    [Fact]
    public async Task Library_unauthorized_access_does_not_leak_file_paths()
    {
        var directory = Directory.CreateTempSubdirectory("ecommerce-tests-").FullName;
        try
        {
            var (status, body) = await Invoke(() =>
            {
                // Bir dizini dosya gibi açmak Windows'ta UnauthorizedAccessException (yol içerir) fırlatır.
                if (OperatingSystem.IsWindows())
                    File.OpenRead(directory).Dispose();
                else
                    throw CreateFrameworkUnauthorizedException(directory);
                return Task.CompletedTask;
            });

            status.Should().Be(500);
            body.GetRawText().Should().NotContain(Path.GetFileName(directory));
        }
        finally
        {
            Directory.Delete(directory);
        }
    }

    [Fact]
    public async Task Unknown_exceptions_return_generic_500()
    {
        var (status, body) = await Invoke(() => throw new ApplicationException("connection string: Password=secret"));

        status.Should().Be(500);
        body.GetRawText().Should().NotContain("secret");
    }

    private static UnauthorizedAccessException CreateFrameworkUnauthorizedException(string path)
    {
        try
        {
            // Framework kodundan gelen gerçek bir örnek elde etmek için.
            System.Reflection.MethodInfo method = typeof(File).GetMethod(nameof(File.ReadAllText), [typeof(string)])!;
            method.Invoke(null, [path]);
        }
        catch (System.Reflection.TargetInvocationException e) when (e.InnerException is UnauthorizedAccessException inner)
        {
            return inner;
        }
        catch (System.Reflection.TargetInvocationException)
        {
        }

        throw new InvalidOperationException("Platformda framework kaynaklı UnauthorizedAccessException üretilemedi.");
    }

    private static async Task<(int Status, JsonElement Body)> Invoke(Func<Task> next)
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var middleware = new GlobalExceptionMiddleware(_ => next(), NullLogger<GlobalExceptionMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        context.Response.Body.Position = 0;
        context.Response.ContentType.Should().Be("application/problem+json");
        var body = await JsonDocument.ParseAsync(context.Response.Body);
        return (context.Response.StatusCode, body.RootElement.Clone());
    }
}
