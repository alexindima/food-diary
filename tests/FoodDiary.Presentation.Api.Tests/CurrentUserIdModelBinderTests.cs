using System.Security.Claims;
using FoodDiary.Presentation.Api.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace FoodDiary.Presentation.Api.Tests;

public sealed class CurrentUserIdModelBinderTests {
    [Fact]
    public async Task BindModelAsync_WithValidUserClaim_BindsGuid() {
        var binder = new CurrentUserIdModelBinder();
        var userGuid = Guid.NewGuid();
        var httpContext = new DefaultHttpContext {
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, userGuid.ToString())], "test")),
        };
        var bindingContext = CreateBindingContext(httpContext);

        await binder.BindModelAsync(bindingContext);

        Assert.True(bindingContext.Result.IsModelSet);
        Assert.Equal(userGuid, Assert.IsType<Guid>(bindingContext.Result.Model));
    }

    [Fact]
    public async Task BindModelAsync_WithoutCurrentUser_ThrowsCurrentUserUnavailableException() {
        var binder = new CurrentUserIdModelBinder();
        var httpContext = new DefaultHttpContext();
        var bindingContext = CreateBindingContext(httpContext);

        await Assert.ThrowsAsync<CurrentUserUnavailableException>(() => binder.BindModelAsync(bindingContext));
    }

    [Fact]
    public async Task BindModelAsync_WithSubClaim_BindsGuid() {
        var binder = new CurrentUserIdModelBinder();
        var userGuid = Guid.NewGuid();
        var httpContext = new DefaultHttpContext {
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", userGuid.ToString())], "test")),
        };
        var bindingContext = CreateBindingContext(httpContext);

        await binder.BindModelAsync(bindingContext);

        Assert.True(bindingContext.Result.IsModelSet);
        Assert.Equal(userGuid, Assert.IsType<Guid>(bindingContext.Result.Model));
    }

    private static DefaultModelBindingContext CreateBindingContext(HttpContext httpContext, Type? modelType = null) {
        var metadataProvider = new EmptyModelMetadataProvider();
        var effectiveModelType = modelType ?? typeof(Guid);

        return new DefaultModelBindingContext {
            ActionContext = new Microsoft.AspNetCore.Mvc.ActionContext {
                HttpContext = httpContext,
            },
            ModelMetadata = metadataProvider.GetMetadataForType(effectiveModelType),
            ModelName = "currentUserId",
            ModelState = new ModelStateDictionary(),
            ValueProvider = new CompositeValueProvider(),
            ValidationState = new ValidationStateDictionary(),
        };
    }
}
