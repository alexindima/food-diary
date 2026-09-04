using FoodDiary.Application.Abstractions.Common.Validation;
using FoodDiary.Results;

namespace FoodDiary.Application.Tests.Validation;

[ExcludeFromCodeCoverage]
public sealed class DietologistParserTests {
    [Fact]
    public void RequiredId_RejectsEmptyWithoutCallingFactory_AndPreservesCustomError() {
        var error = new Error("Id.Empty", "Missing identifier.", ErrorKind.Validation);
        int factoryCalls = 0;
        Result<Guid> result = DietologistRequiredIdParser.Parse(Guid.Empty, error, value => {
            factoryCalls++;
            return value;
        });

        Assert.Multiple(
            () => Assert.Equal(0, factoryCalls),
            () => Assert.Equal(error, ResultAssert.Failure(result)),
            () => Assert.Equal(error, ResultAssert.Failure(DietologistRequiredIdParser.ToFailure(result))),
            () => Assert.Equal(error, ResultAssert.Failure(DietologistRequiredIdParser.ToFailure<string, Guid>(result))));
    }

    [Fact]
    public void RequiredId_PreservesValueAndLiteralValidationError() {
        var id = Guid.NewGuid();
        Result<Guid> valid = DietologistRequiredIdParser.Parse(id, "clientId", "Required.", value => value);
        Error error = ResultAssert.Failure(DietologistRequiredIdParser.Parse(Guid.Empty, "clientId", "Required.", value => value));
        Assert.Multiple(
            () => Assert.Equal(id, ResultAssert.Success(valid)),
            () => Assert.Equal("Validation.Invalid", error.Code),
            () => Assert.Equal("Field clientId is invalid: Required.", error.Message),
            () => Assert.Equal(ErrorKind.Validation, error.Kind),
            () => Assert.Equal(new[] { "Field clientId is invalid: Required." }, error.Details!["clientId"]));
    }

    [Fact]
    public void EnumParser_PreservesNumericAndDefinedValueDistinction() {
        Assert.Multiple(
            () => Assert.Equal((DayOfWeek)999, ResultAssert.Success(DietologistEnumValueParser.ParseRequired<DayOfWeek>("999", "day", "Invalid."))),
            () => Assert.True(DietologistEnumValueParser.CanParse<DayOfWeek>("999")),
            () => Assert.False(DietologistEnumValueParser.CanParseDefined<DayOfWeek>("999")),
            () => Assert.True(DietologistEnumValueParser.CanParseOptional<DayOfWeek>(" ")),
            () => Assert.Null(ResultAssert.Success(DietologistEnumValueParser.ParseOptional<DayOfWeek>(value: null, "day", "Invalid."))));
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
