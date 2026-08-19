using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text.Json;
using FoodDiary.Presentation.Api.Features.Logs.Requests;

namespace FoodDiary.Presentation.Api.Tests;

[ExcludeFromCodeCoverage]
public sealed class ClientTelemetryLogHttpRequestValidationTests {
    public static TheoryData<string, string> SupportedEvents => new() {
        { "client_error", "global-error" },
        { "http_request", "api.request" },
        { "route_timing", "router.navigation" },
        { "web_vital", "ttfb" },
        { "web_vital", "fcp" },
        { "web_vital", "lcp" },
        { "user_action", "notifications.settings.viewed" },
        { "user_action", "notifications.preference.changed" },
        { "user_action", "notifications.subscription.ensure" },
        { "user_action", "notifications.subscription.remove" },
        { "user_action", "notifications.test-push.schedule" },
        { "user_action", "fasting.session.started" },
    };

    public static TheoryData<string> InvalidDetailsJson => [
        JsonSerializer.Serialize(new string('x', 4097)),
        "{\"a\":{\"b\":{\"c\":{\"d\":{\"e\":1}}}}}",
        $"{{\"{new string('p', 65)}\":1}}",
        $"[{string.Join(',', Enumerable.Repeat("1", 17))}]",
        JsonSerializer.Serialize(new string('s', 513)),
        $"{{{string.Join(',', Enumerable.Range(0, 65).Select(index => "\"p" + index.ToString(CultureInfo.InvariantCulture) + "\":null"))}}}",
    ];

    [Theory]
    [MemberData(nameof(SupportedEvents))]
    public void Validate_WithSupportedEvent_ReturnsNoErrors(string category, string name) {
        ClientTelemetryLogHttpRequest request = CreateValidRequest() with {
            Category = category,
            Name = name,
        };

        IReadOnlyList<ValidationResult> errors = Validate(request);

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_WithAllSupportedOptionalValues_ReturnsNoErrors() {
        JsonElement details = JsonSerializer.Deserialize<JsonElement>("""
            {"array":[1,true,false,null,"text"],"object":{"value":2}}
            """);
        ClientTelemetryLogHttpRequest request = CreateValidRequest() with {
            Message = new string('m', 512),
            Location = new string('l', 512),
            Route = new string('r', 256),
            PageRoute = new string('p', 256),
            SessionId = new string('s', 96),
            HttpMethod = new string('h', 16),
            Outcome = new string('o', 32),
            DurationMs = 600_000,
            Value = 42,
            StatusCode = 599,
            Unit = new string('u', 16),
            BuildVersion = new string('b', 64),
            Stack = new string('t', 1024),
            Details = details,
        };

        IReadOnlyList<ValidationResult> errors = Validate(request);

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_WithInvalidScalarValues_ReportsEveryAffectedMember() {
        ClientTelemetryLogHttpRequest request = CreateValidRequest() with {
            Category = " ",
            Name = new string('n', 65),
            Level = "debug",
            Timestamp = "not-a-date",
            Message = new string('m', 513),
            Location = new string('l', 513),
            Route = new string('r', 257),
            PageRoute = new string('p', 257),
            SessionId = new string('s', 97),
            HttpMethod = new string('h', 17),
            Outcome = new string('o', 33),
            DurationMs = double.NaN,
            Value = double.PositiveInfinity,
            StatusCode = 600,
            Unit = new string('u', 17),
            BuildVersion = new string('b', 65),
            Stack = new string('t', 1025),
        };

        IReadOnlyList<ValidationResult> errors = Validate(request);

        string[] expectedMembers = [
            "Category", "Name", "Level", "Timestamp", "Message", "Location", "Route", "PageRoute",
            "SessionId", "HttpMethod", "Outcome", "DurationMs", "Value", "StatusCode", "Unit",
            "BuildVersion", "Stack",
        ];
        Assert.All(expectedMembers, member => Assert.Contains(
            errors,
            error => error.MemberNames.Contains(member, StringComparer.Ordinal)));
    }

    [Fact]
    public void Validate_WithTimestampBeyondClockSkew_IsInvalid() {
        DateTimeOffset nowUtc = new(2026, 6, 30, 10, 15, 0, TimeSpan.Zero);
        ClientTelemetryLogHttpRequest request = CreateValidRequest() with {
            Timestamp = nowUtc.AddMinutes(5).AddTicks(1).ToString("O"),
        };

        IReadOnlyList<ValidationResult> errors = Validate(request, nowUtc);

        Assert.Contains(errors, error => error.MemberNames.Contains("Timestamp", StringComparer.Ordinal));
    }

    [Fact]
    public void Validate_WithTimestampAtClockSkewBoundary_IsValid() {
        DateTimeOffset nowUtc = new(2026, 6, 30, 10, 15, 0, TimeSpan.Zero);
        ClientTelemetryLogHttpRequest request = CreateValidRequest() with {
            Timestamp = nowUtc.AddMinutes(5).ToString("O"),
        };

        IReadOnlyList<ValidationResult> errors = Validate(request, nowUtc);

        Assert.Empty(errors);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(600_001)]
    public void Validate_WithDurationOutsideRange_IsInvalid(double durationMs) {
        ClientTelemetryLogHttpRequest request = CreateValidRequest() with { DurationMs = durationMs };

        Assert.Contains(Validate(request), error => error.MemberNames.Contains("DurationMs", StringComparer.Ordinal));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(600)]
    public void Validate_WithStatusOutsideRange_IsInvalid(int statusCode) {
        ClientTelemetryLogHttpRequest request = CreateValidRequest() with { StatusCode = statusCode };

        Assert.Contains(Validate(request), error => error.MemberNames.Contains("StatusCode", StringComparer.Ordinal));
    }

    [Fact]
    public void Validate_WithUndefinedDetails_IsInvalid() {
        ClientTelemetryLogHttpRequest request = CreateValidRequest() with { Details = default(JsonElement) };

        Assert.Contains(Validate(request), error => error.MemberNames.Contains("Details", StringComparer.Ordinal));
    }

    [Theory]
    [MemberData(nameof(InvalidDetailsJson))]
    public void Validate_WithComplexOrOversizedDetails_IsInvalid(string json) {
        JsonElement details = JsonSerializer.Deserialize<JsonElement>(json);
        ClientTelemetryLogHttpRequest request = CreateValidRequest() with { Details = details };

        Assert.Contains(Validate(request), error => error.MemberNames.Contains("Details", StringComparer.Ordinal));
    }

    private static ClientTelemetryLogHttpRequest CreateValidRequest() =>
        new("http_request", "api.request", "info", DateTimeOffset.UtcNow.ToString("O"));

    private static IReadOnlyList<ValidationResult> Validate(
        ClientTelemetryLogHttpRequest request,
        DateTimeOffset? utcNow = null) {
        TimeProvider timeProvider = utcNow.HasValue
            ? new FixedTimeProvider(utcNow.Value)
            : TimeProvider.System;
        var validationContext = new ValidationContext(
            request,
            new TimeProviderServiceProvider(timeProvider),
            items: null);
        return [.. request.Validate(validationContext)];
    }

    [ExcludeFromCodeCoverage]
    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    [ExcludeFromCodeCoverage]
    private sealed class TimeProviderServiceProvider(TimeProvider timeProvider) : IServiceProvider {
        public object? GetService(Type serviceType) =>
            serviceType == typeof(TimeProvider) ? timeProvider : null;
    }
}
