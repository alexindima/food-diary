using FoodDiary.Application.Abstractions.Common.Validation;
using FoodDiary.Results;
using MealsEnumValueParser = FoodDiary.Application.Meals.Common.Validation.EnumValueParser;

namespace FoodDiary.Application.Tests.Validation;

[ExcludeFromCodeCoverage]
public sealed class EnumValueParserTests {
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ParseOptional_WithEmptyInput_ReturnsNull(string? value) {
        DayOfWeek? dietologist = ResultAssert.Success(
            DietologistEnumValueParser.ParseOptional<DayOfWeek>(value, "day", "Invalid day."));
        DayOfWeek? shared = ResultAssert.Success(
            SharedEnumValueParser.ParseOptional<DayOfWeek>(value, "day", "Invalid day."));
        DayOfWeek? meals = ResultAssert.Success(
            MealsEnumValueParser.ParseOptional<DayOfWeek>(value, "day", "Invalid day."));

        Assert.Multiple(
            () => Assert.Null(dietologist),
            () => Assert.Null(shared),
            () => Assert.Null(meals));
    }

    [Fact]
    public void ParseOptional_WithCaseInsensitiveValue_ReturnsParsedEnum() {
        DayOfWeek? dietologist = ResultAssert.Success(
            DietologistEnumValueParser.ParseOptional<DayOfWeek>("mOnDaY", "day", "Invalid day."));
        DayOfWeek? shared = ResultAssert.Success(
            SharedEnumValueParser.ParseOptional<DayOfWeek>("mOnDaY", "day", "Invalid day."));
        DayOfWeek? meals = ResultAssert.Success(
            MealsEnumValueParser.ParseOptional<DayOfWeek>("mOnDaY", "day", "Invalid day."));

        Assert.Multiple(
            () => Assert.Equal(DayOfWeek.Monday, dietologist),
            () => Assert.Equal(DayOfWeek.Monday, shared),
            () => Assert.Equal(DayOfWeek.Monday, meals));
    }

    [Fact]
    public void ParseOptional_WithInvalidValue_ReturnsFieldError() {
        Result<DayOfWeek?> dietologist = DietologistEnumValueParser.ParseOptional<DayOfWeek>(
            "invalid", "day", "Invalid day.");
        Result<DayOfWeek?> shared = SharedEnumValueParser.ParseOptional<DayOfWeek>(
            "invalid", "day", "Invalid day.");
        Result<DayOfWeek?> meals = MealsEnumValueParser.ParseOptional<DayOfWeek>(
            "invalid", "day", "Invalid day.");

        ResultAssert.Failure(dietologist, "Validation.Invalid");
        ResultAssert.Failure(shared, "Validation.Invalid");
        ResultAssert.Failure(meals, "Validation.Invalid");
    }

    [Fact]
    public void DietologistParser_CoversPredicateAndCustomErrorOverloads() {
        var customError = new Error("Custom.Invalid", "Custom enum error.");

        Result<DayOfWeek> valid = DietologistEnumValueParser.ParseRequired<DayOfWeek>("Friday", customError);
        Result<DayOfWeek> invalid = DietologistEnumValueParser.ParseRequired<DayOfWeek>("invalid", customError);

        Assert.Multiple(
            () => Assert.True(DietologistEnumValueParser.CanParse<DayOfWeek>("Monday")),
            () => Assert.False(DietologistEnumValueParser.CanParse<DayOfWeek>("invalid")),
            () => Assert.True(DietologistEnumValueParser.CanParseOptional<DayOfWeek>(value: null)),
            () => Assert.True(DietologistEnumValueParser.CanParseDefined<DayOfWeek>("Tuesday")),
            () => Assert.False(DietologistEnumValueParser.CanParseDefined<DayOfWeek>("999")),
            () => Assert.Equal(DayOfWeek.Friday, ResultAssert.Success(valid)),
            () => Assert.Equal(customError, ResultAssert.Failure(invalid)));
    }
}
