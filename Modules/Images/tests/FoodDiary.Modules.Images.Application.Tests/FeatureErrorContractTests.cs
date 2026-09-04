using FoodDiary.Application.Abstractions.Images.Common;
using FoodDiary.Results;

namespace FoodDiary.Application.Tests;

[ExcludeFromCodeCoverage]
public sealed class FeatureErrorContractTests {
    [Fact]
    public void ImageErrors_HasOwnerAssemblyAndNamespace() {
        Assert.Multiple(
            () => Assert.Equal("FoodDiary.Modules.Images.Application.Abstractions", typeof(ImageErrors).Assembly.GetName().Name),
            () => Assert.Equal("FoodDiary.Application.Abstractions.Images.Common", typeof(ImageErrors).Namespace));
    }

    [Fact]
    public void ImageErrors_PreservesEveryPublicErrorContract() {
        AssertError(ImageErrors.InvalidData("sample"), "Image.InvalidData", "sample", ErrorKind.Validation);
        AssertError(ImageErrors.NotFound(Guid.Parse("12345678-1234-1234-1234-123456789abc")), "Image.NotFound", "Image asset with ID 12345678-1234-1234-1234-123456789abc was not found.", ErrorKind.NotFound);
        AssertError(ImageErrors.Forbidden(), "Image.Forbidden", "Image asset does not belong to the current user.", ErrorKind.Forbidden);
        AssertError(ImageErrors.InUse(), "Image.InUse", "Image asset is already in use.", ErrorKind.Conflict);
        AssertError(ImageErrors.StorageError(), "Image.StorageError", "Image storage operation failed.", ErrorKind.ExternalFailure);
    }

    private static void AssertError(Error error, string code, string message, ErrorKind kind) {
        Assert.Multiple(
            () => Assert.Equal(code, error.Code),
            () => Assert.Equal(message, error.Message),
            () => Assert.Equal(kind, error.Kind),
            () => Assert.Null(error.Details));
    }
}
