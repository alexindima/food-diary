using FoodDiary.Application.Abstractions.Products.Common;
using FoodDiary.Results;

namespace FoodDiary.Application.Tests;

[ExcludeFromCodeCoverage]
public sealed class FeatureErrorContractTests {
    [Fact]
    public void ProductErrors_HasOwnerAssemblyAndNamespace() {
        Assert.Multiple(
            () => Assert.Equal("FoodDiary.Modules.Products.Application.Abstractions", typeof(ProductErrors).Assembly.GetName().Name),
            () => Assert.Equal("FoodDiary.Application.Abstractions.Products.Common", typeof(ProductErrors).Namespace));
    }

    [Fact]
    public void ProductErrors_PreservesEveryPublicErrorContract() {
        AssertError(ProductErrors.NotFound(Guid.Parse("12345678-1234-1234-1234-123456789abc")), "Product.NotFound", "Product with ID 12345678-1234-1234-1234-123456789abc was not found.", ErrorKind.NotFound);
        AssertError(ProductErrors.NotAccessible(Guid.Parse("12345678-1234-1234-1234-123456789abc")), "Product.NotAccessible", "Product with ID 12345678-1234-1234-1234-123456789abc does not belong to the current user or was not found.", ErrorKind.NotFound);
        AssertError(ProductErrors.AlreadyExists("sample"), "Product.AlreadyExists", "Product with barcode sample already exists.", ErrorKind.Conflict);
        AssertError(ProductErrors.InvalidData("sample"), "Product.InvalidData", "sample", ErrorKind.Internal);
    }

    private static void AssertError(Error error, string code, string message, ErrorKind kind) {
        Assert.Multiple(
            () => Assert.Equal(code, error.Code),
            () => Assert.Equal(message, error.Message),
            () => Assert.Equal(kind, error.Kind),
            () => Assert.Null(error.Details));
    }
}
