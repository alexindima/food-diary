using System.Security.Claims;
using FoodDiary.Presentation.Api.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace FoodDiary.Presentation.Api.Tests;

[ExcludeFromCodeCoverage]
public sealed class CurrentRefreshSessionIdModelBinderTests {
    [Fact]
    public async Task BindModelAsync_WithSignedSessionClaim_BindsGuid() {
        var binder = new CurrentRefreshSessionIdModelBinder();
        var sessionId = Guid.NewGuid();
        var httpContext = new DefaultHttpContext {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim("refresh_session_id", sessionId.ToString())],
                "test")),
        };
        DefaultModelBindingContext bindingContext = CreateBindingContext(httpContext);

        await binder.BindModelAsync(bindingContext);

        Assert.True(bindingContext.Result.IsModelSet);
        Assert.Equal(sessionId, Assert.IsType<Guid>(bindingContext.Result.Model));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-guid")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    public async Task BindModelAsync_WithoutValidSessionClaim_ThrowsUnauthorizedBoundary(string? claimValue) {
        Claim[] claims = claimValue is null ? [] : [new Claim("refresh_session_id", claimValue)];
        var httpContext = new DefaultHttpContext {
            User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test")),
        };
        DefaultModelBindingContext bindingContext = CreateBindingContext(httpContext);

        await Assert.ThrowsAsync<CurrentUserUnavailableException>(() =>
            new CurrentRefreshSessionIdModelBinder().BindModelAsync(bindingContext));
    }

    private static DefaultModelBindingContext CreateBindingContext(HttpContext httpContext) {
        var metadataProvider = new EmptyModelMetadataProvider();
        return new DefaultModelBindingContext {
            ActionContext = new Microsoft.AspNetCore.Mvc.ActionContext { HttpContext = httpContext },
            ModelMetadata = metadataProvider.GetMetadataForType(typeof(Guid)),
            ModelName = "currentSessionId",
            ModelState = new ModelStateDictionary(),
            ValueProvider = new CompositeValueProvider(),
            ValidationState = [],
        };
    }
}
