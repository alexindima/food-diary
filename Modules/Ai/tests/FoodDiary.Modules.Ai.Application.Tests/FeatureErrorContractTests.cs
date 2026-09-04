using FoodDiary.Application.Abstractions.Ai.Common;
using FoodDiary.Results;

namespace FoodDiary.Application.Tests;

[ExcludeFromCodeCoverage]
public sealed class FeatureErrorContractTests {
    [Fact]
    public void AiErrors_HasOwnerAssemblyAndNamespace() {
        Assert.Multiple(
            () => Assert.Equal("FoodDiary.Modules.Ai.Application.Abstractions", typeof(AiErrors).Assembly.GetName().Name),
            () => Assert.Equal("FoodDiary.Application.Abstractions.Ai.Common", typeof(AiErrors).Namespace));
    }

    [Fact]
    public void AiErrors_PreservesEveryPublicErrorContract() {
        AssertError(AiErrors.ImageNotFound(Guid.Parse("12345678-1234-1234-1234-123456789abc")), "Ai.ImageNotFound", "Image asset with ID 12345678-1234-1234-1234-123456789abc was not found.", ErrorKind.NotFound);
        AssertError(AiErrors.Forbidden(), "Ai.Forbidden", "Image asset does not belong to the current user.", ErrorKind.Forbidden);
        AssertError(AiErrors.ConsentRequired(), "Ai.ConsentRequired", "AI consent must be accepted before using AI features.", ErrorKind.Forbidden);
        AssertError(AiErrors.EmptyItems(), "Ai.EmptyItems", "No food items were provided.", ErrorKind.Validation);
        AssertError(AiErrors.OpenAiFailed("sample"), "Ai.OpenAiFailed", "sample", ErrorKind.ExternalFailure);
        AssertError(AiErrors.InvalidResponse("sample"), "Ai.InvalidResponse", "sample", ErrorKind.ExternalFailure);
        AssertError(AiErrors.QuotaExceeded(), "Ai.QuotaExceeded", "AI token quota exceeded for the current month.", ErrorKind.RateLimited);
        AssertError(AiErrors.PromptTemplateNotFound(), "Ai.PromptTemplateNotFound", "AI prompt template disappeared during update.", ErrorKind.NotFound);
    }

    private static void AssertError(Error error, string code, string message, ErrorKind kind) {
        Assert.Multiple(
            () => Assert.Equal(code, error.Code),
            () => Assert.Equal(message, error.Message),
            () => Assert.Equal(kind, error.Kind),
            () => Assert.Null(error.Details));
    }
}
