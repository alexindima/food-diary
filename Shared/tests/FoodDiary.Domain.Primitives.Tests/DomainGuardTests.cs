using System.Globalization;
using System.Text.Json;

namespace FoodDiary.Domain.Primitives.Tests;

[ExcludeFromCodeCoverage]
public sealed class DomainGuardTests {
    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void NumericGuards_RejectNonfiniteBeforeRangeChecks(double value) {
        AssertRange(() => DomainGuard.Finite(value, "number"), "Value must be a finite number.");
        AssertRange(() => DomainGuard.NonNegativeFinite(value, "number"), "Value must be a finite number.");
        AssertRange(() => DomainGuard.PositiveFinite(value, "number"), "Value must be a finite number.");
        AssertRange(() => DomainGuard.NonNegativeFinite((double?)value, "number"), "Value must be a finite number.");
        AssertRange(() => DomainGuard.PositiveFinite((double?)value, "number"), "Value must be a finite number.");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-0.001)]
    public void NonNegativeFinite_RejectsNegativeValues(double value) {
        AssertRange(() => DomainGuard.NonNegativeFinite(value, "number"), "Value must be non-negative.");
        AssertRange(() => DomainGuard.NonNegativeFinite((double?)value, "number"), "Value must be non-negative.");
        Assert.Equal(value, DomainGuard.Finite(value, "number"));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    public void PositiveGuards_RejectNonpositiveValues(int value) {
        AssertRange(() => DomainGuard.Positive(value, "number"), "Value must be greater than zero.");
        AssertRange(() => DomainGuard.Positive((int?)value, "number"), "Value must be greater than zero.");
        AssertRange(() => DomainGuard.PositiveFinite(value, "number"), "Value must be greater than zero.");
        AssertRange(() => DomainGuard.PositiveFinite((double?)value, "number"), "Value must be greater than zero.");
    }

    [Fact]
    public void NumericGuards_ReturnValidValuesAndNulls() {
        Assert.Multiple(
            () => Assert.Null(DomainGuard.Positive((int?)null, "number")),
            () => Assert.Null(DomainGuard.PositiveFinite(value: null, paramName: "number")),
            () => Assert.Null(DomainGuard.NonNegativeFinite(value: null, paramName: "number")),
            () => Assert.Equal(0, DomainGuard.NonNegativeFinite(0, "number")),
            () => Assert.Equal(0, DomainGuard.NonNegativeFinite((double?)0, "number")),
            () => Assert.Equal(double.Epsilon, DomainGuard.PositiveFinite(double.Epsilon, "number")),
            () => Assert.Equal(double.MaxValue, DomainGuard.PositiveFinite((double?)double.MaxValue, "number")),
            () => Assert.Equal(int.MaxValue, DomainGuard.Positive(int.MaxValue, "number")),
            () => Assert.Equal(1, DomainGuard.Positive((int?)1, "number")));
    }

    [Fact]
    public void Defined_RequiresDeclaredEnumMember() {
        Assert.Equal(DateTimeKind.Local, DomainGuard.Defined(DateTimeKind.Local, "number"));
        AssertRange(() => DomainGuard.Defined((DateTimeKind)3, "number"), "Value must be one of the supported values.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" \t ")]
    public void TextGuards_PreserveMissingInputBehavior(string? value) {
        ArgumentException exception = Assert.Throws<ArgumentException>(() => DomainGuard.RequiredText(value!, 10, "text"));
        Assert.Equal(ArgumentMessage("text", "Value is required."), exception.Message);
        Assert.Equal("text", exception.ParamName);
        Assert.Null(DomainGuard.OptionalText(value, 10, "text"));
        Assert.Null(DomainGuard.OptionalJson(value, 10, "text"));
    }

    [Fact]
    public void TextGuards_TrimBeforeLengthCheckAndUseInvariantMessages() {
        CultureInfo previous = CultureInfo.CurrentCulture;
        try {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("ar-SA");
            Assert.Equal("ab", DomainGuard.RequiredText(" ab ", 2, "text"));
            Assert.Equal("ab", DomainGuard.OptionalText(" ab ", 2, "text"));
            ArgumentOutOfRangeException required = Assert.Throws<ArgumentOutOfRangeException>(() => DomainGuard.RequiredText(new string('x', 1235), 1234, "text"));
            ArgumentOutOfRangeException optional = Assert.Throws<ArgumentOutOfRangeException>(() => DomainGuard.OptionalText(new string('x', 1235), 1234, "text"));
            string expected = RangeMessage("text", "Value must be at most 1234 characters.");
            Assert.Equal(expected, required.Message);
            Assert.Equal(expected, optional.Message);
            Assert.Equal("text", required.ParamName);
            Assert.Equal("text", optional.ParamName);
        } finally {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Theory]
    [InlineData(" {} ")]
    [InlineData("    ")]
    [InlineData(" xxx ")]
    public void OptionalJson_RejectsRawLengthBeforeTrimOrSyntax(string value) {
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => DomainGuard.OptionalJson(value, 2, "json"));
        Assert.Equal(RangeMessage("json", "Value must be at most 2 characters."), exception.Message);
        Assert.Equal("json", exception.ParamName);
    }

    [Fact]
    public void RequiredJson_TrimsBeforeLengthThenValidatesSyntax() {
        Assert.Equal("{}", DomainGuard.RequiredJson(" {} ", 2, "json"));
        Assert.Throws<ArgumentOutOfRangeException>(() => DomainGuard.RequiredJson(" broken ", 2, "json"));
        ArgumentException exception = Assert.Throws<ArgumentException>(() => DomainGuard.RequiredJson("   ", 2, "json"));
        Assert.Equal(ArgumentMessage("json", "Value is required."), exception.Message);
    }

    [Theory]
    [InlineData("{broken}")]
    [InlineData("{\"a\":1,}")]
    [InlineData("//comment\n{}")]
    public void JsonGuards_PreserveMalformedJsonException(string value) {
        ArgumentException required = Assert.Throws<ArgumentException>(() => DomainGuard.RequiredJson(value, 100, "json"));
        ArgumentException optional = Assert.Throws<ArgumentException>(() => DomainGuard.OptionalJson(value, 100, "json"));
        foreach (ArgumentException exception in new[] { required, optional }) {
            Assert.Equal("json", exception.ParamName);
            Assert.Equal(ArgumentMessage("json", "Value must contain valid JSON."), exception.Message);
            Assert.IsAssignableFrom<JsonException>(exception.InnerException);
        }
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("[]")]
    [InlineData("null")]
    [InlineData("123")]
    [InlineData("true")]
    public void JsonGuards_AcceptAnyValidJsonRoot(string value) {
        Assert.Equal(value, DomainGuard.RequiredJson(value, 100, "json"));
        Assert.Equal(value, DomainGuard.OptionalJson(" " + value + " ", 100, "json"));
    }

    [Theory]
    [InlineData(DateTimeKind.Utc)]
    [InlineData(DateTimeKind.Local)]
    public void UtcGuards_ConvertSpecifiedKinds(DateTimeKind kind) {
        var value = new DateTime(2026, 1, 15, 12, 30, 0, kind);
        Assert.Equal(value.ToUniversalTime(), DomainGuard.RequiredUtc(value, "time"));
        Assert.Equal(DateTimeKind.Utc, DomainGuard.RequiredUtc(value, "time").Kind);
        Assert.Equal(value.ToUniversalTime(), DomainGuard.OptionalUtc(value, "time"));
    }

    [Fact]
    public void UtcGuards_RejectUnspecifiedButAcceptNull() {
        Assert.Null(DomainGuard.OptionalUtc(value: null, paramName: "time"));
        var value = new DateTime(2026, 1, 1);
        ArgumentOutOfRangeException required = Assert.Throws<ArgumentOutOfRangeException>(() => DomainGuard.RequiredUtc(value, "time"));
        ArgumentOutOfRangeException optional = Assert.Throws<ArgumentOutOfRangeException>(() => DomainGuard.OptionalUtc(value, "time"));
        Assert.Equal("time", required.ParamName);
        Assert.Equal(RangeMessage("time", "UTC timestamp kind must be specified."), required.Message);
        Assert.Equal(required.Message, optional.Message);
    }

    private static void AssertRange(Action action, string message) {
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(action);
        Assert.Equal("number", exception.ParamName);
        Assert.Equal(RangeMessage("number", message), exception.Message);
    }

    private static string RangeMessage(string paramName, string message) => new ArgumentOutOfRangeException(paramName, message).Message;

    private static string ArgumentMessage(string paramName, string message) => new ArgumentException(message, paramName).Message;
}
