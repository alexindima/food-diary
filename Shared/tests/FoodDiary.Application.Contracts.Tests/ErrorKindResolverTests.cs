using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Results;

namespace FoodDiary.Application.Contracts.Tests;

[ExcludeFromCodeCoverage]
public sealed class ErrorKindResolverTests {
    [Fact]
    public void ErrorKindResolver_ResolvesKnownFallbackPatterns() {
        Assert.Null(ErrorKindResolver.Resolve(errorCode: null));
        Assert.Null(ErrorKindResolver.Resolve(" "));
        Assert.Equal(ErrorKind.Forbidden, ErrorKindResolver.Resolve("Authentication.AdminSsoForbidden"));
        Assert.Equal(ErrorKind.Unauthorized, ErrorKindResolver.Resolve("Authentication.Unknown"));
        Assert.Equal(ErrorKind.Validation, ErrorKindResolver.Resolve("Validation.Invalid"));
        Assert.Equal(ErrorKind.NotFound, ErrorKindResolver.Resolve("Product.NotAccessible"));
        Assert.Equal(ErrorKind.NotFound, ErrorKindResolver.Resolve("User.NotFound"));
        Assert.Equal(ErrorKind.Conflict, ErrorKindResolver.Resolve("Recipe.AlreadyExists"));
        Assert.Null(ErrorKindResolver.Resolve("Custom.Unknown"));
    }
}
