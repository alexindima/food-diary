using FoodDiary.Results;

namespace FoodDiary.Modules.OpenFoodFacts.Application.Tests.Support;

[ExcludeFromCodeCoverage]
public static class ResultAssert {
    public static void Success(Result result) {
        Assert.True(result.IsSuccess, $"Expected success, but got '{result.Error.Code}': {result.Error.Message}");
    }
}
