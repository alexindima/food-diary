using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Common.Time;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.Common;

[ExcludeFromCodeCoverage]
public class CommonValidationTests {
    [Fact]
    public void UserIdParser_WithNullOrEmpty_ReturnsInvalidToken() {
        var nullResult = UserIdParser.Parse(null);
        var emptyResult = UserIdParser.Parse(Guid.Empty);

        Assert.True(nullResult.IsFailure);
        Assert.True(emptyResult.IsFailure);
        Assert.Equal(Errors.Authentication.InvalidToken.Code, nullResult.Error.Code);
        Assert.Equal(Errors.Authentication.InvalidToken.Code, emptyResult.Error.Code);
    }

    [Fact]
    public void UserIdParser_WithValue_ReturnsUserId() {
        var value = Guid.NewGuid();

        var result = UserIdParser.Parse(value);

        Assert.True(result.IsSuccess);
        Assert.Equal(new UserId(value), result.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void StringCodeParser_ParseOptionalLanguage_WithBlankValue_ReturnsNull(string? value) {
        var result = StringCodeParser.ParseOptionalLanguage(value, "language", "invalid language");

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value);
    }

    [Fact]
    public void StringCodeParser_ParseOptionalCodes_WithSupportedValues_ReturnsNormalizedCode() {
        var language = StringCodeParser.ParseOptionalLanguage("ru", "language", "invalid language");
        var gender = StringCodeParser.ParseOptionalGender("f", "gender", "invalid gender");
        var theme = StringCodeParser.ParseOptionalTheme("dark", "theme", "invalid theme");
        var uiStyle = StringCodeParser.ParseOptionalUiStyle("modern", "uiStyle", "invalid ui style");

        Assert.True(language.IsSuccess);
        Assert.True(gender.IsSuccess);
        Assert.True(theme.IsSuccess);
        Assert.True(uiStyle.IsSuccess);
        Assert.Equal("ru", language.Value);
        Assert.Equal("F", gender.Value);
        Assert.Equal("dark", theme.Value);
        Assert.Equal("modern", uiStyle.Value);
    }

    [Fact]
    public void StringCodeParser_ParseOptionalCodes_WithUnsupportedValues_ReturnsValidationFailure() {
        var language = StringCodeParser.ParseOptionalLanguage("de", "language", "invalid language");
        var gender = StringCodeParser.ParseOptionalGender("unknown", "gender", "invalid gender");
        var theme = StringCodeParser.ParseOptionalTheme("neon", "theme", "invalid theme");
        var uiStyle = StringCodeParser.ParseOptionalUiStyle("retro", "uiStyle", "invalid ui style");

        Assert.All([language, gender, theme, uiStyle], result => {
            Assert.True(result.IsFailure);
            Assert.Equal("Validation.Invalid", result.Error.Code);
        });
    }

    [Fact]
    public void StringCodeParser_ParseRequiredLanguage_ReturnsSuccessOrValidationFailure() {
        var success = StringCodeParser.ParseRequiredLanguage("en", "language", "invalid language");
        var failure = StringCodeParser.ParseRequiredLanguage("de-DE", "language", "invalid language");

        Assert.True(success.IsSuccess);
        Assert.Equal("en", success.Value);
        Assert.True(failure.IsFailure);
        Assert.Equal("Validation.Invalid", failure.Error.Code);
    }

    [Fact]
    public void EnumValueParser_ParseOptional_ReturnsNullParsedValueOrFailure() {
        var blank = EnumValueParser.ParseOptional<MealType>(" ", "mealType", "invalid meal type");
        var parsed = EnumValueParser.ParseOptional<MealType>("lunch", "mealType", "invalid meal type");
        var invalid = EnumValueParser.ParseOptional<MealType>("snack-time", "mealType", "invalid meal type");

        Assert.True(blank.IsSuccess);
        Assert.Null(blank.Value);
        Assert.True(parsed.IsSuccess);
        Assert.Equal(MealType.Lunch, parsed.Value);
        Assert.True(invalid.IsFailure);
        Assert.Equal("Validation.Invalid", invalid.Error.Code);
    }

    [Fact]
    public void EnumValueParser_ParseRequired_ReturnsParsedValueOrFailure() {
        var parsed = EnumValueParser.ParseRequired<MealType>("Dinner", "mealType", "invalid meal type");
        var invalid = EnumValueParser.ParseRequired<MealType>(null, "mealType", "invalid meal type");

        Assert.True(parsed.IsSuccess);
        Assert.Equal(MealType.Dinner, parsed.Value);
        Assert.True(invalid.IsFailure);
        Assert.Equal("Validation.Invalid", invalid.Error.Code);
    }

    [Fact]
    public void OptionalEntityIdValidator_OnlyRejectsExplicitEmptyGuid() {
        var nullResult = OptionalEntityIdValidator.EnsureNotEmpty(null, "productId", "Product id");
        var valueResult = OptionalEntityIdValidator.EnsureNotEmpty(Guid.NewGuid(), "productId", "Product id");
        var emptyResult = OptionalEntityIdValidator.EnsureNotEmpty(Guid.Empty, "productId", "Product id");

        Assert.True(nullResult.IsSuccess);
        Assert.True(valueResult.IsSuccess);
        Assert.True(emptyResult.IsFailure);
        Assert.Equal("Validation.Invalid", emptyResult.Error.Code);
        Assert.Contains("Product id", emptyResult.Error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ImageAssetIdParser_ReturnsNullParsedValueOrFailure() {
        var blank = ImageAssetIdParser.ParseOptional(null, "imageAssetId");
        var id = Guid.NewGuid();
        var parsed = ImageAssetIdParser.ParseOptional(id, "imageAssetId");
        var invalid = ImageAssetIdParser.ParseOptional(Guid.Empty, "imageAssetId");

        Assert.True(blank.IsSuccess);
        Assert.Null(blank.Value);
        Assert.True(parsed.IsSuccess);
        Assert.Equal(new ImageAssetId(id), parsed.Value);
        Assert.True(invalid.IsFailure);
        Assert.Equal("Validation.Invalid", invalid.Error.Code);
    }

    [Fact]
    public void UtcDateNormalizer_NormalizeDatePreservingUnspecifiedAsUtc_HandlesAllDateKinds() {
        var utc = new DateTime(2026, 6, 4, 15, 30, 0, DateTimeKind.Utc);
        var unspecified = new DateTime(2026, 6, 4, 15, 30, 0, DateTimeKind.Unspecified);
        var local = DateTime.SpecifyKind(new DateTime(2026, 6, 4, 15, 30, 0), DateTimeKind.Local);

        Assert.Equal(new DateTime(2026, 6, 4, 0, 0, 0, DateTimeKind.Utc), UtcDateNormalizer.NormalizeDatePreservingUnspecifiedAsUtc(utc));
        Assert.Equal(new DateTime(2026, 6, 4, 0, 0, 0, DateTimeKind.Utc), UtcDateNormalizer.NormalizeDatePreservingUnspecifiedAsUtc(unspecified));
        Assert.Equal(
            DateTime.SpecifyKind(local.ToUniversalTime().Date, DateTimeKind.Utc),
            UtcDateNormalizer.NormalizeDatePreservingUnspecifiedAsUtc(local));
    }

    [Fact]
    public void UtcDateNormalizer_NormalizeInstantPreservingUnspecifiedAsUtc_HandlesAllDateKinds() {
        var utc = new DateTime(2026, 6, 4, 15, 30, 0, DateTimeKind.Utc);
        var unspecified = new DateTime(2026, 6, 4, 15, 30, 0, DateTimeKind.Unspecified);
        var local = DateTime.SpecifyKind(new DateTime(2026, 6, 4, 15, 30, 0), DateTimeKind.Local);

        Assert.Equal(utc, UtcDateNormalizer.NormalizeInstantPreservingUnspecifiedAsUtc(utc));
        Assert.Equal(DateTime.SpecifyKind(unspecified, DateTimeKind.Utc), UtcDateNormalizer.NormalizeInstantPreservingUnspecifiedAsUtc(unspecified));
        Assert.Equal(local.ToUniversalTime(), UtcDateNormalizer.NormalizeInstantPreservingUnspecifiedAsUtc(local));
    }

    [Fact]
    public void UtcDateNormalizer_LocalFallbackMethods_ReturnUtcDateBoundaries() {
        var value = new DateTime(2026, 6, 4, 15, 30, 0, DateTimeKind.Utc);

        var start = UtcDateNormalizer.NormalizeDateUsingLocalFallback(value);
        var end = UtcDateNormalizer.NormalizeDateEndUsingLocalFallback(value);

        Assert.Equal(new DateTime(2026, 6, 4, 0, 0, 0, DateTimeKind.Utc), start);
        Assert.Equal(new DateTime(2026, 6, 5, 0, 0, 0, DateTimeKind.Utc).AddTicks(-1), end);
    }

    [Fact]
    public void UtcDateNormalizer_LocalFallbackMethods_WithLocalValue_UseUniversalDateBoundaries() {
        var value = DateTime.SpecifyKind(new DateTime(2026, 6, 4, 23, 30, 0), DateTimeKind.Local);
        var utcDate = value.ToUniversalTime().Date;

        var start = UtcDateNormalizer.NormalizeDateUsingLocalFallback(value);
        var end = UtcDateNormalizer.NormalizeDateEndUsingLocalFallback(value);

        Assert.Equal(DateTime.SpecifyKind(utcDate, DateTimeKind.Utc), start);
        Assert.Equal(DateTime.SpecifyKind(utcDate.AddDays(1).AddTicks(-1), DateTimeKind.Utc), end);
    }
}
