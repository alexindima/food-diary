using FoodDiary.Application.Abstractions.Fasting.Common;
using FoodDiary.Results;

namespace FoodDiary.Application.Tests;

[ExcludeFromCodeCoverage]
public sealed class FastingErrorContractTests {
    [Fact]
    public void Factory_IsOwnedByModule() {
        Assert.Equal("FoodDiary.Modules.Fasting.Application.Abstractions", typeof(FastingErrors).Assembly.GetName().Name);
        Assert.Equal("FoodDiary.Application.Abstractions.Fasting.Common", typeof(FastingErrors).Namespace);
    }

    [Fact]
    public void Errors_PreserveClassificationAndCustomMessage() {
        AssertError(FastingErrors.AlreadyActive, "Fasting.AlreadyActive", "A fasting session is already active.", ErrorKind.Conflict);
        AssertError(FastingErrors.NoActiveSession, "Fasting.NoActiveSession", "No active fasting session found.", ErrorKind.NotFound);
        AssertError(FastingErrors.InvalidProtocol, "Fasting.InvalidProtocol", "Invalid fasting protocol.", ErrorKind.Validation);
        AssertError(FastingErrors.InvalidCyclicAction("custom reason"), "Fasting.InvalidCyclicAction", "custom reason", ErrorKind.Validation);
    }

    private static void AssertError(Error error, string code, string message, ErrorKind kind) {
        Assert.Multiple(
            () => Assert.Equal(code, error.Code),
            () => Assert.Equal(message, error.Message),
            () => Assert.Equal(kind, error.Kind),
            () => Assert.Null(error.Details));
    }
}
