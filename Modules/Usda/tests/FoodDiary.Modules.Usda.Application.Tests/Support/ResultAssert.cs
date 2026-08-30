using FoodDiary.Results;

namespace FoodDiary.Application.Tests.Support;

[ExcludeFromCodeCoverage]
public static class ResultAssert {
    public static void Success(Result result) =>
        Assert.True(result.IsSuccess, $"Expected success, got '{result.Error.Code}': {result.Error.Message}");

    public static TValue Success<TValue>(Result<TValue> result) {
        Success((Result)result);
        return result.Value;
    }

    public static Error Failure(Result result) {
        Assert.True(result.IsFailure, "Expected failure, got success.");
        return result.Error;
    }
}
