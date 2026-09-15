using FoodDiary.Modules.Cycles.Contracts.Common;
using FoodDiary.Results;

namespace FoodDiary.Modules.Cycles.Application.Tests;

[ExcludeFromCodeCoverage]
public sealed class FeatureErrorContractTests {
    [Fact]
    public void CycleErrors_HasOwnerAssemblyAndNamespace() {
        Assert.Multiple(
            () => Assert.Equal("FoodDiary.Modules.Cycles.Contracts", typeof(CycleErrors).Assembly.GetName().Name),
            () => Assert.Equal("FoodDiary.Modules.Cycles.Contracts.Common", typeof(CycleErrors).Namespace));
    }

    [Fact]
    public void CycleErrors_PreservesEveryPublicErrorContract() {
        AssertError(CycleErrors.NotFound(Guid.Parse("12345678-1234-1234-1234-123456789abc")), "Cycle.NotFound", "Cycle with ID 12345678-1234-1234-1234-123456789abc was not found.", ErrorKind.NotFound);
    }

    private static void AssertError(Error error, string code, string message, ErrorKind kind) {
        Assert.Multiple(
            () => Assert.Equal(code, error.Code),
            () => Assert.Equal(message, error.Message),
            () => Assert.Equal(kind, error.Kind),
            () => Assert.Null(error.Details));
    }
}
