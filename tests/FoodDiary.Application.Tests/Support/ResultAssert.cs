using FoodDiary.Application.Abstractions.Common.Abstractions.Results;

namespace FoodDiary.Application.Tests;

[ExcludeFromCodeCoverage]
public static class ResultAssert {
    public static void Success(Result result) {
        Assert.True(
            result.IsSuccess,
            $"Expected a successful result, but got '{result.Error.Code}': {result.Error.Message}");
    }

    public static TValue Success<TValue>(Result<TValue> result) {
        Success((Result)result);
        return result.Value;
    }

    public static Error Failure(Result result, string? expectedCode = null) {
        Assert.True(result.IsFailure, "Expected a failed result, but got success.");

        if (expectedCode is not null) {
            Assert.Equal(expectedCode, result.Error.Code);
        }

        return result.Error;
    }
}
