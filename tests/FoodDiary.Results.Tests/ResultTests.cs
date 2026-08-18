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
    public void Failure_WhenErrorIsNull_Throws() {
        Assert.Throws<ArgumentNullException>(() => Result.Failure(error: null!));
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
    public void FailureGeneric_WhenErrorIsNull_Throws() {
        Assert.Throws<ArgumentNullException>(() => Result.Failure<string>(error: null!));
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

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Failure_WhenErrorCodeIsBlank_Throws(string code) {
        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() =>
            Result.Failure(new Error(code, "Message")));

        Assert.Equal("A failed result must contain a non-empty error code.", ex.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Failure_WhenErrorMessageIsBlank_Throws(string message) {
        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() =>
            Result.Failure(new Error("Code", message)));

        Assert.Equal("A failed result must contain a non-empty error message.", ex.Message);
    }

    [Fact]
    public void ErrorNone_HasEmptyCodeAndMessageWithoutKindOrDetails() {
        Assert.Equal(string.Empty, Error.None.Code);
        Assert.Equal(string.Empty, Error.None.Message);
        Assert.Null(Error.None.Kind);
        Assert.Null(Error.None.Details);
    }

    [Fact]
    public void Error_StoresKindAndSnapshotsDetails() {
        var details = new Dictionary<string, string[]>(StringComparer.Ordinal) {
            ["Name"] = ["Required"],
        };

        var error = new Error("Validation.Invalid", "Invalid input.", ErrorKind.Validation, Details: details);

        Assert.Equal("Validation.Invalid", error.Code);
        Assert.Equal("Invalid input.", error.Message);
        Assert.Equal(ErrorKind.Validation, error.Kind);
        Assert.NotSame(details, error.Details);
        Assert.Equal(["Required"], error.Details!["Name"]);

        details["Name"][0] = "Changed";
        details["Other"] = ["Added"];

        Assert.Equal(["Required"], error.Details["Name"]);
        Assert.DoesNotContain("Other", error.Details.Keys, StringComparer.Ordinal);

        string[] exposedMessages = error.Details["Name"];
        exposedMessages[0] = "Mutated";

        Assert.Equal(["Required"], error.Details["Name"]);
    }

    [Fact]
    public void Error_DetailsImplementsReadOnlyDictionaryContract() {
        var error = new Error(
            "Validation.Invalid",
            "Invalid input.",
            Details: new Dictionary<string, string[]>(StringComparer.Ordinal) {
                ["Name"] = ["Required"],
            });
        IReadOnlyDictionary<string, string[]> details = error.Details!;

        Assert.Multiple(
            () => Assert.True(details.Count == 1),
            () => Assert.Contains("Name", details.Keys, StringComparer.Ordinal),
            () => Assert.Equal(["Required"], Assert.Single(details.Values)),
            () => Assert.True(details.ContainsKey("Name")),
            () => Assert.False(details.ContainsKey("Other")),
            () => Assert.Equal(["Required"], Assert.Single(details).Value));

        Assert.True(details.TryGetValue("Name", out string[]? messages));
        Assert.Equal(["Required"], messages);
        Assert.False(details.TryGetValue("Other", out string[]? missingMessages));
        Assert.NotNull(missingMessages);
        Assert.Empty(missingMessages);

        System.Collections.IEnumerator enumerator = ((System.Collections.IEnumerable)details).GetEnumerator();
        Assert.True(enumerator.MoveNext());
    }

    [Fact]
    public void Error_WhenCodeIsNull_Throws() {
        Assert.Throws<ArgumentNullException>(() => new Error(null!, "Message"));
    }

    [Fact]
    public void Error_WhenMessageIsNull_Throws() {
        Assert.Throws<ArgumentNullException>(() => new Error("Code", null!));
    }

    [Fact]
    public void Error_WithExpressionRevalidatesCodeAndMessage() {
        var error = new Error("Code", "Message");
        Error updated = error with { Code = "Updated.Code", Message = "Updated message." };

        Assert.Multiple(
            () => Assert.Equal("Updated.Code", updated.Code),
            () => Assert.Equal("Updated message.", updated.Message),
            () => Assert.Throws<ArgumentNullException>(() => error with { Code = null! }),
            () => Assert.Throws<ArgumentNullException>(() => error with { Message = null! }));
    }

    [Fact]
    public void Error_WithExpressionSnapshotsDetails() {
        var details = new Dictionary<string, string[]>(StringComparer.Ordinal) {
            ["Name"] = ["Required"],
        };
        Error error = Error.None with { Details = details };

        details["Name"][0] = "Changed";
        string[] exposedMessages = error.Details!["Name"];
        exposedMessages[0] = "Mutated";

        Assert.Equal(["Required"], error.Details["Name"]);
    }

    [Fact]
    public void Error_EqualityUsesDetailsContent() {
        var left = new Error(
            "Validation.Invalid",
            "Invalid input.",
            ErrorKind.Validation,
            new Dictionary<string, string[]>(StringComparer.Ordinal) {
                ["Name"] = ["Required"],
                ["Email"] = ["Invalid"],
            });
        var equivalent = new Error(
            "Validation.Invalid",
            "Invalid input.",
            ErrorKind.Validation,
            new Dictionary<string, string[]>(StringComparer.Ordinal) {
                ["Email"] = ["Invalid"],
                ["Name"] = ["Required"],
            });
        Error sharedDetailsCopy = left with { Message = left.Message };
        var differentCount = new Error(
            "Validation.Invalid",
            "Invalid input.",
            ErrorKind.Validation,
            new Dictionary<string, string[]>(StringComparer.Ordinal) {
                ["Name"] = ["Required"],
            });
        var differentKey = new Error(
            "Validation.Invalid",
            "Invalid input.",
            ErrorKind.Validation,
            new Dictionary<string, string[]>(StringComparer.Ordinal) {
                ["Other"] = ["Required"],
                ["Email"] = ["Invalid"],
            });
        var differentMessage = new Error(
            "Validation.Invalid",
            "Invalid input.",
            ErrorKind.Validation,
            new Dictionary<string, string[]>(StringComparer.Ordinal) {
                ["Name"] = ["Changed"],
                ["Email"] = ["Invalid"],
            });

        Assert.Multiple(
            () => Assert.Equal(left, equivalent),
            () => Assert.Equal(left.GetHashCode(), equivalent.GetHashCode()),
            () => Assert.Equal(left, sharedDetailsCopy),
            () => Assert.NotEqual(left, differentCount),
            () => Assert.NotEqual(left, differentKey),
            () => Assert.NotEqual(left, differentMessage),
            () => Assert.NotEqual(left, new Error(left.Code, left.Message, left.Kind, new Dictionary<string, string[]>(StringComparer.Ordinal))),
            () => Assert.True(left.Details!.Equals(left.Details)),
            () => Assert.False(left.Details!.Equals(null)),
            () => Assert.False(left.Details!.Equals("not details")));
    }

    [Fact]
    public void Error_WhenDetailMessagesAreNull_Throws() {
        var details = new Dictionary<string, string[]>(StringComparer.Ordinal) {
            ["Name"] = null!,
        };

        Assert.Throws<ArgumentException>(() => new Error("Code", "Message", Details: details));
    }

    [Fact]
    public void Error_WhenDetailContainsNullMessage_Throws() {
        var details = new Dictionary<string, string[]>(StringComparer.Ordinal) {
            ["Name"] = [null!],
        };

        Assert.Throws<ArgumentException>(() => new Error("Code", "Message", Details: details));
    }

    [Fact]
    public void Error_LegacyStringConversion_ReturnsCode() {
        var error = new Error("Custom.Code", "Custom message.");

#pragma warning disable CS0618 // Verify the compatibility path while directing production callers to Error.Code.
        string code = error;
#pragma warning restore CS0618

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
