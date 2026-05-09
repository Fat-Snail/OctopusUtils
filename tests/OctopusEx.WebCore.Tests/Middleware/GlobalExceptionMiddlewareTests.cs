namespace OctopusEx.WebCore.Tests.Middleware;

using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using OctopusEx.WebCore.Exceptions;
using OctopusEx.WebCore.Middleware;

public class GlobalExceptionMiddlewareTests
{
    private static GlobalExceptionMiddleware Build(RequestDelegate next, String envName = "Production")
    {
        var env = new TestHostEnvironment { EnvironmentName = envName };
        var jsonOptions = Options.Create(new JsonOptions());
        return new GlobalExceptionMiddleware(next, NullLogger<GlobalExceptionMiddleware>.Instance, env, jsonOptions);
    }

    private static DefaultHttpContext NewContext()
    {
        var ctx = new DefaultHttpContext();
        ctx.Response.Body = new MemoryStream();
        return ctx;
    }

    private static async Task<JsonElement> ReadBody(HttpContext ctx)
    {
        ctx.Response.Body.Seek(0, SeekOrigin.Begin);
        using var doc = await JsonDocument.ParseAsync(ctx.Response.Body);
        return doc.RootElement.Clone();
    }

    [Theory]
    [InlineData(typeof(UnauthorizedException), 401)]
    [InlineData(typeof(ForbiddenException), 403)]
    [InlineData(typeof(NotFoundException), 404)]
    [InlineData(typeof(KeyNotFoundException), 404)]
    [InlineData(typeof(ValidationException), 422)]
    [InlineData(typeof(ArgumentException), 400)]
    public async Task Maps_KnownExceptions_ToCorrectStatusCode(Type exceptionType, Int32 expectedStatus)
    {
        var exception = (Exception)Activator.CreateInstance(exceptionType, "boom")!;
        var middleware = Build(_ => throw exception);
        var ctx = NewContext();

        await middleware.InvokeAsync(ctx);

        ctx.Response.StatusCode.Should().Be(expectedStatus);
    }

    [Fact]
    public async Task UnknownException_Returns500_WithGenericMessageInProduction()
    {
        var middleware = Build(_ => throw new InvalidOperationException("internal detail"), envName: "Production");
        var ctx = NewContext();

        await middleware.InvokeAsync(ctx);

        ctx.Response.StatusCode.Should().Be(500);
        var body = await ReadBody(ctx);
        body.GetProperty("message").GetString().Should().Be("服务器内部错误");
    }

    [Fact]
    public async Task UnknownException_InDevelopment_IncludesExceptionDetails()
    {
        var middleware = Build(_ => throw new InvalidOperationException("internal detail"), envName: "Development");
        var ctx = NewContext();

        await middleware.InvokeAsync(ctx);

        var body = await ReadBody(ctx);
        body.GetProperty("message").GetString().Should().Contain("InvalidOperationException");
        body.GetProperty("message").GetString().Should().Contain("internal detail");
    }

    [Fact]
    public async Task FieldValidationException_Returns422_WithErrorsField()
    {
        var errors = new Dictionary<String, String[]> { ["email"] = new[] { "格式错误" } };
        var middleware = Build(_ => throw new FieldValidationException(errors));
        var ctx = NewContext();

        await middleware.InvokeAsync(ctx);

        ctx.Response.StatusCode.Should().Be(422);
        var body = await ReadBody(ctx);
        body.GetProperty("errors").GetProperty("email")[0].GetString().Should().Be("格式错误");
    }

    [Fact]
    public async Task ResponseAlreadyStarted_RethrowsException()
    {
        var middleware = Build(_ => throw new InvalidOperationException("late"));
        var ctx = NewContext();
        ctx.Features.Set<IHttpResponseFeature>(new FakeStartedResponseFeature());

        var act = async () => await middleware.InvokeAsync(ctx);
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("late");
    }

    private sealed class FakeStartedResponseFeature : IHttpResponseFeature
    {
        public Int32 StatusCode { get; set; } = 200;
        public String? ReasonPhrase { get; set; }
        public IHeaderDictionary Headers { get; set; } = new HeaderDictionary();
        public Stream Body { get; set; } = new MemoryStream();
        public Boolean HasStarted => true;
        public void OnStarting(Func<Object, Task> callback, Object state) { }
        public void OnCompleted(Func<Object, Task> callback, Object state) { }
    }

    [Fact]
    public async Task ClientCanceled_DoesNotReturn500()
    {
        var middleware = Build(_ => throw new OperationCanceledException());
        var ctx = NewContext();
        var cts = new CancellationTokenSource();
        cts.Cancel();
        ctx.RequestAborted = cts.Token;

        await middleware.InvokeAsync(ctx);

        ctx.Response.StatusCode.Should().Be(499);
    }

    [Fact]
    public async Task ErrorResponse_IncludesTraceId()
    {
        var middleware = Build(_ => throw new NotFoundException("missing"));
        var ctx = NewContext();
        ctx.TraceIdentifier = "trace-123";

        await middleware.InvokeAsync(ctx);

        var body = await ReadBody(ctx);
        body.TryGetProperty("traceId", out var traceId).Should().BeTrue();
        traceId.GetString().Should().NotBeNullOrEmpty();
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public String EnvironmentName { get; set; } = Environments.Production;
        public String ApplicationName { get; set; } = "Tests";
        public String ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = null!;
    }
}
