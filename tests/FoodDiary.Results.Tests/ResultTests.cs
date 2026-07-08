namespace FoodDiary.Results.Tests;

[ExcludeFromCodeCoverage]
public sealed class ResultTests {
    [Fact]
    public void Success_CreatesSuccessfulResultWithoutError() {
        var result = Result.Success();

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal(Error.None, result.Error);
    }

    [Fact]
    public void Failure_CreatesFailedResultWithError() {
        var error = new Error("Validation.Invalid", "Invalid input.", ErrorKind.Validation);

        var result = Result.Failure(error);

        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal(error, result.Error);
    }

    [Fact]
    public void SuccessGeneric_CreatesSuccessfulResultWithValue() {
        var result = Result.Success("value");

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal(Error.None, result.Error);
        Assert.Equal("value", result.Value);
    }

    [Fact]
    public void SuccessGeneric_AllowsNullValueForNullableType() {
        var result = Result.Success<string?>(value: null);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value);
    }

    [Fact]
    public void FailureGeneric_CreatesFailedResultWithError() {
        var error = new Error("User.NotFound", "User was not found.", ErrorKind.NotFound);

        var result = Result.Failure<string>(error);

        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal(error, result.Error);
    }

    [Fact]
    public void FailureGeneric_ValueThrows() {
        var result = Result.Failure<string>(new Error("User.NotFound", "User was not found.", ErrorKind.NotFound));

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => result.Value);

        Assert.Equal("Value is unavailable for a failed result.", ex.Message);
    }

    [Fact]
    public void GenericResult_ImplicitValueConversion_ReturnsSuccess() {
        Result<string> result = "value";

        Assert.True(result.IsSuccess);
        Assert.Equal("value", result.Value);
    }

    [Fact]
    public void Constructor_WhenSuccessfulResultContainsError_Throws() {
        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() =>
            new ExposedResult(isSuccess: true, new Error("Invalid", "Invalid.")));

        Assert.Equal("A successful result cannot contain an error.", ex.Message);
    }

    [Fact]
    public void Constructor_WhenFailedResultContainsNoError_Throws() {
        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() =>
            new ExposedResult(isSuccess: false, Error.None));

        Assert.Equal("A failed result must contain an error.", ex.Message);
    }

    [Fact]
    public void ErrorNone_HasEmptyCodeAndMessageWithoutKindOrDetails() {
        Assert.Equal(string.Empty, Error.None.Code);
        Assert.Equal(string.Empty, Error.None.Message);
        Assert.Null(Error.None.Kind);
        Assert.Null(Error.None.Details);
    }

    [Fact]
    public void Error_StoresKindAndDetails() {
        var details = new Dictionary<string, string[]>(StringComparer.Ordinal) {
            ["Name"] = ["Required"],
        };

        var error = new Error("Validation.Invalid", "Invalid input.", ErrorKind.Validation, Details: details);

        Assert.Equal("Validation.Invalid", error.Code);
        Assert.Equal("Invalid input.", error.Message);
        Assert.Equal(ErrorKind.Validation, error.Kind);
        Assert.Same(details, error.Details);
    }

    [Fact]
    public void Error_ImplicitStringConversion_ReturnsCode() {
        string code = new Error("Custom.Code", "Custom message.");

        Assert.Equal("Custom.Code", code);
    }

    [Theory]
    [InlineData(ErrorKind.Validation, 0)]
    [InlineData(ErrorKind.Unauthorized, 1)]
    [InlineData(ErrorKind.Forbidden, 2)]
    [InlineData(ErrorKind.NotFound, 3)]
    [InlineData(ErrorKind.Conflict, 4)]
    [InlineData(ErrorKind.RateLimited, 5)]
    [InlineData(ErrorKind.ExternalFailure, 6)]
    [InlineData(ErrorKind.Internal, 7)]
    public void ErrorKind_ValuesRemainStable(ErrorKind kind, int expectedValue) {
        Assert.Equal(expectedValue, (int)kind);
    }

    [ExcludeFromCodeCoverage]
    private sealed class ExposedResult(bool isSuccess, Error error) : Result(isSuccess, error);
}
