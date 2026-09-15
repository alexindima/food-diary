using FoodDiary.Modules.Meals.Application.Common.Validation;
using FoodDiary.Results;
using ResultAssert = FoodDiary.Testing.Assertions.ResultAssert;

namespace FoodDiary.Modules.Meals.Application.Tests.Validation;

[ExcludeFromCodeCoverage]
public sealed class EnumValueParserTests {
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ParseOptional_WithEmptyInput_ReturnsNull(string? value) {
        DayOfWeek? parsed = ResultAssert.Success(EnumValueParser.ParseOptional<DayOfWeek>(value, "day", "Invalid day."));

        Assert.Null(parsed);
    }

    [Fact]
    public void ParseOptional_WithCaseInsensitiveValue_ReturnsParsedEnum() {
        DayOfWeek? parsed = ResultAssert.Success(EnumValueParser.ParseOptional<DayOfWeek>("mOnDaY", "day", "Invalid day."));

        Assert.Equal(DayOfWeek.Monday, parsed);
    }

    [Fact]
    public void ParseOptional_WithInvalidValue_ReturnsFieldError() {
        Result<DayOfWeek?> result = EnumValueParser.ParseOptional<DayOfWeek>("invalid", "day", "Invalid day.");

        ResultAssert.Failure(result, "Validation.Invalid");
    }
}
