using System.Security.Claims;
using System.Text.Json;
using FoodDiary.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Presentation.Api.Responses;
using FoodDiary.Presentation.Api.Security;
using FoodDiary.Web.Api.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace FoodDiary.Web.Api.IntegrationTests.Extensions;

public sealed class ImpersonationAccessGuardMiddlewareTests {
    [Fact]
    public async Task InvokeAsync_WithProtectedEndpointAndImpersonatedUser_ReturnsForbiddenErrorContract() {
        var context = CreateContext(hasProtectedEndpoint: true, isImpersonated: true);
        var nextCalled = false;
        var middleware = new ImpersonationAccessGuardMiddleware(_ => {
            nextCalled = true;
            return Task.CompletedTask;
        }, NullLogger<ImpersonationAccessGuardMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        Assert.False(nextCalled);
        Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);

        context.Response.Body.Position = 0;
        var payload = await JsonSerializer.DeserializeAsync<ApiErrorHttpResponse>(
            context.Response.Body,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(payload);
        Assert.Equal("Authentication.ImpersonationActionForbidden", payload.Error);
        Assert.False(string.IsNullOrWhiteSpace(payload.TraceId));
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public async Task InvokeAsync_WhenEndpointIsNotProtectedOrUserIsNotImpersonated_CallsNext(
        bool hasProtectedEndpoint,
        bool isImpersonated) {
        var context = CreateContext(hasProtectedEndpoint, isImpersonated);
        var nextCalled = false;
        var middleware = new ImpersonationAccessGuardMiddleware(_ => {
            nextCalled = true;
            return Task.CompletedTask;
        }, NullLogger<ImpersonationAccessGuardMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        Assert.True(nextCalled);
    }

    private static DefaultHttpContext CreateContext(bool hasProtectedEndpoint, bool isImpersonated) {
        var context = new DefaultHttpContext {
            Response = {
                Body = new MemoryStream(),
            },
        };

        context.TraceIdentifier = "trace-id";
        context.Request.Method = HttpMethods.Post;
        context.Request.Path = "/api/v1/users/password";

        var claims = new List<Claim> {
            new(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
        };

        if (isImpersonated) {
            claims.Add(new Claim(JwtImpersonationClaimNames.IsImpersonation, "true"));
            claims.Add(new Claim(JwtImpersonationClaimNames.ActorUserId, Guid.NewGuid().ToString()));
        }

        context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));

        if (hasProtectedEndpoint) {
            context.SetEndpoint(new Endpoint(
                _ => Task.CompletedTask,
                new EndpointMetadataCollection(new BlockImpersonatedAccessAttribute()),
                "protected"));
        }

        return context;
    }
}
