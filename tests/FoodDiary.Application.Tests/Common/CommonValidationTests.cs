using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Admin.Common;
using FoodDiary.Results;
using FoodDiary.Application.Common.Time;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Images.Common;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.Common;

[ExcludeFromCodeCoverage]
public class CommonValidationTests {
    [Fact]
    public void UserIdParser_WithNullOrEmpty_ReturnsInvalidToken() {
        Result<UserId> nullResult = UserIdParser.Parse(null);
        Result<UserId> emptyResult = UserIdParser.Parse(Guid.Empty);

        ResultAssert.Failure(nullResult);
        ResultAssert.Failure(emptyResult);
        Assert.Equal(Errors.Authentication.InvalidToken.Code, nullResult.Error.Code);
        Assert.Equal(Errors.Authentication.InvalidToken.Code, emptyResult.Error.Code);
    }

    [Fact]
    public void UserIdParser_WithValue_ReturnsUserId() {
        var value = Guid.NewGuid();

        Result<UserId> result = UserIdParser.Parse(value);

        ResultAssert.Success(result);
        Assert.Equal(new UserId(value), result.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void UserPreferenceCodeParser_ParseOptionalLanguage_WithBlankValue_ReturnsNull(string? value) {
        Result<string?> result = UserPreferenceCodeParser.ParseOptionalLanguage(value, "language", "invalid language");

        ResultAssert.Success(result);
        Assert.Null(result.Value);
    }

    [Fact]
    public void UserPreferenceCodeParser_ParseOptionalCodes_WithSupportedValues_ReturnsNormalizedCode() {
        Result<string?> language = UserPreferenceCodeParser.ParseOptionalLanguage("ru", "language", "invalid language");
        Result<string?> gender = UserPreferenceCodeParser.ParseOptionalGender("f", "gender", "invalid gender");
        Result<string?> theme = UserPreferenceCodeParser.ParseOptionalTheme("dark", "theme", "invalid theme");
        Result<string?> uiStyle = UserPreferenceCodeParser.ParseOptionalUiStyle("modern", "uiStyle", "invalid ui style");

        ResultAssert.Success(language);
        ResultAssert.Success(gender);
        ResultAssert.Success(theme);
        ResultAssert.Success(uiStyle);
        Assert.Equal("ru", language.Value);
        Assert.Equal("F", gender.Value);
        Assert.Equal("dark", theme.Value);
        Assert.Equal("modern", uiStyle.Value);
    }

    [Fact]
    public void UserPreferenceCodeParser_ParseOptionalCodes_WithUnsupportedValues_ReturnsValidationFailure() {
        Result<string?> language = UserPreferenceCodeParser.ParseOptionalLanguage("de", "language", "invalid language");
        Result<string?> gender = UserPreferenceCodeParser.ParseOptionalGender("unknown", "gender", "invalid gender");
        Result<string?> theme = UserPreferenceCodeParser.ParseOptionalTheme("neon", "theme", "invalid theme");
        Result<string?> uiStyle = UserPreferenceCodeParser.ParseOptionalUiStyle("retro", "uiStyle", "invalid ui style");

        Assert.All([language, gender, theme, uiStyle], result => {
            ResultAssert.Failure(result);
            Assert.Equal("Validation.Invalid", result.Error.Code);
        });
    }

    [Fact]
    public void AdminLocaleParser_ParseRequiredLanguage_ReturnsSuccessOrValidationFailure() {
        Result<string> success = AdminLocaleParser.ParseRequiredLanguage("en", "language", "invalid language");
        Result<string> failure = AdminLocaleParser.ParseRequiredLanguage("de-DE", "language", "invalid language");

        ResultAssert.Success(success);
        Assert.Equal("en", success.Value);
        ResultAssert.Failure(failure);
        Assert.Equal("Validation.Invalid", failure.Error.Code);
    }

    [Fact]
    public void EnumValueParser_ParseOptional_ReturnsNullParsedValueOrFailure() {
        Result<MealType?> blank = EnumValueParser.ParseOptional<MealType>(" ", "mealType", "invalid meal type");
        Result<MealType?> parsed = EnumValueParser.ParseOptional<MealType>("lunch", "mealType", "invalid meal type");
        Result<MealType?> invalid = EnumValueParser.ParseOptional<MealType>("snack-time", "mealType", "invalid meal type");

        ResultAssert.Success(blank);
        Assert.Null(blank.Value);
        ResultAssert.Success(parsed);
        Assert.Equal(MealType.Lunch, parsed.Value);
        ResultAssert.Failure(invalid);
        Assert.Equal("Validation.Invalid", invalid.Error.Code);
    }

    [Fact]
    public void EnumValueParser_ParseRequired_ReturnsParsedValueOrFailure() {
        Result<MealType> parsed = EnumValueParser.ParseRequired<MealType>("Dinner", "mealType", "invalid meal type");
        Result<MealType> invalid = EnumValueParser.ParseRequired<MealType>(value: null, "mealType", "invalid meal type");

        ResultAssert.Success(parsed);
        Assert.Equal(MealType.Dinner, parsed.Value);
        ResultAssert.Failure(invalid);
        Assert.Equal("Validation.Invalid", invalid.Error.Code);
    }

    [Fact]
    public void OptionalEntityIdValidator_OnlyRejectsExplicitEmptyGuid() {
        Result nullResult = OptionalEntityIdValidator.EnsureNotEmpty(value: null, "productId", "Product id");
        Result valueResult = OptionalEntityIdValidator.EnsureNotEmpty(Guid.NewGuid(), "productId", "Product id");
        Result emptyResult = OptionalEntityIdValidator.EnsureNotEmpty(Guid.Empty, "productId", "Product id");

        ResultAssert.Success(nullResult);
        ResultAssert.Success(valueResult);
        ResultAssert.Failure(emptyResult);
        Assert.Equal("Validation.Invalid", emptyResult.Error.Code);
        Assert.Contains("Product id", emptyResult.Error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ImageAssetIdParser_ReturnsNullParsedValueOrFailure() {
        Result<ImageAssetId?> blank = ImageAssetIdParser.ParseOptional(value: null, "imageAssetId");
        var id = Guid.NewGuid();
        Result<ImageAssetId?> parsed = ImageAssetIdParser.ParseOptional(id, "imageAssetId");
        Result<ImageAssetId?> invalid = ImageAssetIdParser.ParseOptional(Guid.Empty, "imageAssetId");

        ResultAssert.Success(blank);
        Assert.Null(blank.Value);
        ResultAssert.Success(parsed);
        Assert.Equal(new ImageAssetId(id), parsed.Value);
        ResultAssert.Failure(invalid);
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

        DateTime start = UtcDateNormalizer.NormalizeDateUsingLocalFallback(value);
        DateTime end = UtcDateNormalizer.NormalizeDateEndUsingLocalFallback(value);

        Assert.Equal(new DateTime(2026, 6, 4, 0, 0, 0, DateTimeKind.Utc), start);
        Assert.Equal(new DateTime(2026, 6, 5, 0, 0, 0, DateTimeKind.Utc).AddTicks(-1), end);
    }

    [Fact]
    public void UtcDateNormalizer_LocalFallbackMethods_WithLocalValue_UseUniversalDateBoundaries() {
        var value = DateTime.SpecifyKind(new DateTime(2026, 6, 4, 23, 30, 0), DateTimeKind.Local);
        DateTime utcDate = value.ToUniversalTime().Date;

        DateTime start = UtcDateNormalizer.NormalizeDateUsingLocalFallback(value);
        DateTime end = UtcDateNormalizer.NormalizeDateEndUsingLocalFallback(value);

        Assert.Equal(DateTime.SpecifyKind(utcDate, DateTimeKind.Utc), start);
        Assert.Equal(DateTime.SpecifyKind(utcDate.AddDays(1).AddTicks(-1), DateTimeKind.Utc), end);
    }
}
