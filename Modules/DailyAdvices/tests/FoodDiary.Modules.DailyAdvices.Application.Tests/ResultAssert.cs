using FoodDiary.Results;

namespace FoodDiary.Application.Tests;

[ExcludeFromCodeCoverage]
internal static class ResultAssert {
    public static void Success<T>(Result<T> result) => Assert.True(result.IsSuccess, result.Error.ToString());
    public static void Failure<T>(Result<T> result) => Assert.True(result.IsFailure, "Expected failure result.");
}
